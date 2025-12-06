using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FS_ShooterSystem
{
    public class ShooterUI : MonoBehaviour
    {
        [Header("Ammo UI")]
        public TextMeshProUGUI totalAmmoCount;
        public Image infiniteImage;
        public TextMeshProUGUI loadedAmmoCount;

        [Header("Weapon UI")]
        public Image currentWeaponImage;
        public GameObject weaponUI;
        public ShooterCrosshairController crosshairController;

        [Header("Grenade UI")]
        public GameObject grenadeUI;
        public Image grenadeTimerImage;
        public TextMeshProUGUI grenadeTimerText;
        public Image grenadeImage;

        [Header("Trajectory Settings")]
        [SerializeField] private LineRenderer trajectoryLine;
        [SerializeField] private GameObject impactMarkerPrefab;

        private Coroutine grenadeTimerCoroutine;
        private GameObject impactMarker;
        [HideInInspector] public Vector3 cameraForwardHit;
        private Vector3 hitPoint;
        private bool hasValidHit = false;

        private int linePoints = 100;
        private float timeStep = 0.025f;
        private float maxTime = 10f;

        public static ShooterUI i;

        public Action OnGrenadeExplode;

        private void Awake()
        {
            i = this;

            SetImpactMarker();

            if (trajectoryLine == null)
            {
                trajectoryLine = gameObject.AddComponent<LineRenderer>();
                trajectoryLine.startWidth = 0.1f;
                trajectoryLine.endWidth = 0.05f;
                trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
                trajectoryLine.startColor = Color.white;
                trajectoryLine.endColor = new Color(1f, 1f, 1f, 0.5f);
            }

            HideTrajectory();
        }

        #region Ammo & Weapon UI

        public void UpdateShooterUI(ShooterWeaponObject weapon)
        {
            if (weapon != null)
            {
                loadedAmmoCount.text = weapon.CurrentAmmoCount.ToString();
                if (weapon.ParentShooterFighter.hasInfiniteAmmo)
                {
                    infiniteImage.gameObject.SetActive(true);
                    totalAmmoCount.gameObject.SetActive(false);
                }
                else
                {
                    infiniteImage.gameObject.SetActive(false);
                    totalAmmoCount.gameObject.SetActive(true);
                    totalAmmoCount.text = weapon.TotalAmmoCount.ToString();
                }
            }
        }

        #endregion

        #region Grenade UI

        public void StartThrowableItemTimer(ThrowableItem throwableItem)
        {
            if (throwableItem == null || throwableItem.ammo == null)
                return;
            grenadeTimerImage.gameObject.SetActive(true);
            grenadeUI.SetActive(true);

            if (grenadeTimerCoroutine != null)
            {
                StopCoroutine(grenadeTimerCoroutine);
            }

            grenadeTimerCoroutine = StartCoroutine(StartGrenadeTimerUI(throwableItem));
        }

        private IEnumerator StartGrenadeTimerUI(ThrowableItem grenade)
        {
            grenadeImage.sprite = grenade.icon;

            float grenadeCookingTime = grenade.ammo.timer;
            float timer = 0f;

            while (timer <= grenadeCookingTime)
            {
                grenadeTimerImage.fillAmount = Mathf.Lerp(1, 0, timer / grenadeCookingTime);
                grenadeTimerText.text = $"{(grenadeCookingTime - timer):0.0}s";
                timer += Time.deltaTime;
                yield return null;
            }

            grenadeUI.SetActive(false);
            grenadeTimerImage.fillAmount = 1;
            grenadeTimerText.text = "0";
            OnGrenadeExplode?.Invoke();
        }

        public void StopGrenadeTimerUI(ThrowableItem throwableItem)
        {
            if (throwableItem == null || throwableItem.ammo == null)
                return;
            grenadeTimerImage.gameObject.SetActive(false);
            grenadeUI.SetActive(false);

            if (grenadeTimerCoroutine != null)
            {
                StopCoroutine(grenadeTimerCoroutine);
            }
        }

        #endregion

        #region Trajectory UI

        public void SetImpactMarker()
        {
            if (impactMarker == null && impactMarkerPrefab != null)
            {
                impactMarker = Instantiate(impactMarkerPrefab);
                impactMarker.SetActive(false);
            }
        }

        public void UpdateTrajectory(GameObject playerCamera, ShooterFighter shooter)
        {
            //Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            // Updated trajectory code following the proper flow
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            var o = shooter.transform.position + shooter.transform.forward * 2;
            ray.origin = new Vector3(o.x, ray.origin.y, o.z);

            // 1. Calculate the maximum throw distance based on current throw parameters
            float maxThrowDistance = shooter.CurrentThrowableItem.maxThrowDistance;

            // 2. Use the calculated point as the aim target
            Vector3 aimTarget = ray.origin + ray.direction * maxThrowDistance;

            // Check if there's an obstacle in the camera's forward direction (for visual feedback only)
            //bool hasObstacle = Physics.Raycast(ray, out RaycastHit obstacleHit, maxThrowDistance,
            //    ~(1 << shooter.gameObject.layer), QueryTriggerInteraction.Ignore);

            // Set the aim target and mark as valid
            cameraForwardHit = aimTarget;
            hasValidHit = true;

            // 3. Compute the initial velocity required to reach that aim point
            Vector3 throwStartPos = shooter.CurrentThrowableItemObject.CurrentEquippedAmmo.transform.position;
            var velocity = shooter.CalculateVelocityFromTargetPoint(throwStartPos, aimTarget);

            // 4. Perform raycasts along the trajectory using the computed velocity to detect collisions
            var trajectoryPoints = PredictTrajectoryWithCollision(shooter, throwStartPos, velocity);

            // 5. Visualize the trajectory, ending either at collision point or target point if unobstructed
            if (trajectoryPoints != null && trajectoryPoints.Count > 3)
            {
                // Remove first few points to avoid showing trajectory too close to player
                var displayPoints = new List<Vector3>(trajectoryPoints);
                displayPoints.RemoveRange(0, 3);

                trajectoryLine.positionCount = displayPoints.Count;
                trajectoryLine.SetPositions(displayPoints.ToArray());
                trajectoryLine.enabled = true;

                // Set hit point to the final trajectory point (either collision or target)
                hitPoint = trajectoryPoints[trajectoryPoints.Count - 1];
            }
            else
            {
                trajectoryLine.enabled = false;
                hitPoint = aimTarget; // Fallback to aim target
            }

            // Update impact marker at the final hit point
            if (impactMarker != null)
            {
                impactMarker.transform.position = hitPoint;
                impactMarker.SetActive(true);

                // Calculate impact rotation based on trajectory direction
                if (trajectoryPoints != null && trajectoryPoints.Count > 1)
                {
                    int lastIndex = trajectoryPoints.Count - 1;
                    Vector3 impactDirection = trajectoryPoints[lastIndex] - trajectoryPoints[lastIndex - 1];

                    // Raycast to get surface normal at impact point
                    if (Physics.Raycast(trajectoryPoints[lastIndex] - impactDirection.normalized * 0.1f,
                                       impactDirection.normalized, out RaycastHit impactHit, 0.5f,
                                       ~(1 << shooter.gameObject.layer), QueryTriggerInteraction.Ignore))
                    {
                        impactMarker.transform.rotation = Quaternion.FromToRotation(Vector3.up, impactHit.normal);
                    }
                    else
                    {
                        impactMarker.transform.rotation = Quaternion.identity;
                    }
                }
                else
                {
                    impactMarker.transform.rotation = Quaternion.identity;
                }
            }

            // Set final aim point for shooter
            shooter.TargetAimPoint = GetAimPoint(throwStartPos, hitPoint, velocity);
        }
        private List<Vector3> PredictTrajectoryWithCollision(ShooterFighter shooter, Vector3 startPoint, Vector3 initialVelocity)
        {
            var rb = shooter.CurrentThrowableItemObject.CurrentEquippedAmmo.GetComponent<Rigidbody>();
            if (rb == null) return null;

            List<Vector3> trajectoryPoints = new List<Vector3> { startPoint };

            Vector3 position = startPoint;
            Vector3 velocity = initialVelocity;

            for (int i = 0; i < linePoints; i++)
            {
                // Apply physics forces
                Vector3 dragForce = -rb.drag * velocity.magnitude * velocity.normalized;
                Vector3 acceleration = Physics.gravity + (dragForce / rb.mass);

                Vector3 prevPos = position;

                // Update velocity and position
                velocity += acceleration * timeStep;
                position += velocity * timeStep;

                trajectoryPoints.Add(position);

                // Perform raycast along this trajectory segment to detect collisions
                Vector3 segmentDirection = position - prevPos;
                float segmentDistance = segmentDirection.magnitude;

                if (segmentDistance > 0.001f) // Avoid division by zero
                {
                    if (Physics.Raycast(prevPos, segmentDirection.normalized, out RaycastHit hit,
                        segmentDistance, ~(1 << gameObject.layer), QueryTriggerInteraction.Ignore))
                    {
                        // Collision detected - replace last point with hit point and stop
                        trajectoryPoints[i + 1] = hit.point;
                        break;
                    }
                }

                // Stop if we've exceeded maximum simulation time
                if (i * timeStep > maxTime)
                    break;
            }

            return trajectoryPoints;
        }

        public Vector3 GetAimPoint(Vector3 startPoint, Vector3 endPoint, Vector3 velocity)
        {
            float gravity = Mathf.Abs(Physics.gravity.y);
            float timeToPeak = velocity.y / gravity;

            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            Vector3 peakHorizontalPosition = startPoint + horizontalVelocity * timeToPeak;
            float peakY = startPoint.y + (velocity.y * timeToPeak) - (0.5f * gravity * timeToPeak * timeToPeak);

            return new Vector3(peakHorizontalPosition.x, peakY, peakHorizontalPosition.z);
        }

        public void HideTrajectory()
        {
            trajectoryLine.enabled = false;

            if (impactMarker != null)
            {
                impactMarker.SetActive(false);
            }
        }

        #endregion

        #region Crosshair
        public void SetCrosshairActive(bool showCrosshairLines, bool showEntireCrosshair = true)
        {
            if (crosshairController != null)
            {
                crosshairController.HandleCrosshairVisiblity(showCrosshairLines, showEntireCrosshair);
            }
        }
        #endregion
    }
}
