using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace FS_ThirdPerson
{
    public enum Mask { Null, Arm, UpperBody, RightHand, LeftHand, UpperBodyWithRoot, RightArm, UpperBodyWithoutArm }

    public enum UpdateMode { Normal, Unscaled };

    public class AnimGraph : MonoBehaviour
    {
        PlayableGraph graph;
        AnimationPlayableOutput output;
        AnimationMixerPlayable actionMixer;
        AnimationMixerPlayable nonLoopActionMixer;
        AnimationMixerPlayable loopActionMixer;
        Playable animatorMixer;
        Playable animatorOutput;
        Playable defaultAnimatorOutput;
        AnimatorControllerPlayable playableAnimator;

        public Animator animator;
        AvatarMask armMask;
        AvatarMask upperBodyMask;
        AvatarMask rightHandMask;
        AvatarMask upperBodyWithRoot;
        AvatarMask rightArm;
        AvatarMask leftHandMask;
        AvatarMask upperBodyWithoutArm;
        bool currentAnimationIsAdditive = false;
        UpdateMode updateMode;

        public PlayableGraph Graph => graph;

        public float DeltaTime => updateMode == UpdateMode.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;

        public CurrentClipStateInfo currentClipStateInfo = new CurrentClipStateInfo();
        public static KeyValuePair<string, float> TimeScaleOwner = new KeyValuePair<string, float>("", 1);

        public class CurrentClipStateInfo
        {
            public AnimGraphClipInfo currentClipInfo;
            public Playable currentMixer;
            public float normalizedTime = 0f;
            public float timer = 0f;
            public float deltaTime = 0f;
            public float clipLength = 0f;
            public float n;
            public bool isPlayingAnimation => currentClipInfo != null;
        }
        public AnimGraphClipInfo currentClipInfo => currentClipStateInfo.currentClipInfo;

        Playable currentPlayableMixer;
        public void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            CreateGraph();
        }

        public void Update()
        {
            if (animator != null)
            {
                if (playableAnimator.GetSpeed() != animator.speed)
                    playableAnimator.SetSpeed(animator.speed);

                SaveTransitionData();
            }
        }

        private void OnDestroy()
        {
            graph.Destroy();
        }

        #region Create Graph

        void CreateGraph(AnimatorOverrideController runtimeAnimatorController = null)
        {
            if (graph.IsValid())
            {
                graph.Destroy();
            }

            graph = PlayableGraph.Create(gameObject.name + " graph");
            output = AnimationPlayableOutput.Create(graph, "Animation", animator);


            playableAnimator = AnimatorControllerPlayable.Create(graph, runtimeAnimatorController == null ? animator.runtimeAnimatorController : runtimeAnimatorController);
            actionMixer = AnimationMixerPlayable.Create(graph, 2);
            nonLoopActionMixer = AnimationMixerPlayable.Create(graph, 2);
            loopActionMixer = AnimationMixerPlayable.Create(graph, 2);
            defaultAnimatorOutput = animatorOutput = AnimationMixerPlayable.Create(graph, 2);
            animatorMixer = AnimationMixerPlayable.Create(graph, 3);

            graph.Connect(playableAnimator, 0, animatorOutput, 0);
            graph.Connect(animatorOutput, 0, animatorMixer, 0);
            graph.Connect(animatorMixer, 0, loopActionMixer, 0);
            graph.Connect(loopActionMixer, 0, nonLoopActionMixer, 0);
            graph.Connect(nonLoopActionMixer, 0, actionMixer, 0);

            animatorOutput.SetInputWeight(0, 1);
            animatorOutput.SetInputWeight(1, 0);

            animatorMixer.SetInputWeight(0, 1);
            animatorMixer.SetInputWeight(1, 0);
            animatorMixer.SetInputWeight(2, 0);

            loopActionMixer.SetInputWeight(0, 1);
            loopActionMixer.SetInputWeight(1, 0);

            nonLoopActionMixer.SetInputWeight(0, 1);
            nonLoopActionMixer.SetInputWeight(1, 0);

            actionMixer.SetInputWeight(0, 1);
            actionMixer.SetInputWeight(1, 0);

            output.SetSourcePlayable(actionMixer);

            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            graph.Play();

            CreateDefaultMask();
        }
        void CreateDefaultMask()
        {
            armMask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                armMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);

            armMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            armMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            armMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);
            armMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            armMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            armMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);

            rightArm = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                rightArm.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);

            rightArm.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            rightArm.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            rightArm.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);

            upperBodyMask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                upperBodyMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);

            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);

            rightHandMask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                rightHandMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            rightHandMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            rightHandMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);

            leftHandMask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                leftHandMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            leftHandMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            leftHandMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);

            upperBodyWithRoot = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                upperBodyWithRoot.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);

            upperBodyWithRoot.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            upperBodyWithRoot.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            upperBodyWithRoot.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            upperBodyWithRoot.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            upperBodyWithRoot.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);
            upperBodyWithRoot.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            upperBodyWithRoot.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            upperBodyWithRoot.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            upperBodyWithRoot.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, true);

            upperBodyWithoutArm = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                upperBodyWithoutArm.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);

            upperBodyWithoutArm.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            upperBodyWithoutArm.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
        }

        #endregion

        #region Crossfade methods

        public void Crossfade(AnimationClip clip, AnimGraphClipInfo clipInfo = null, bool transitionBack = true, Mask mask = Mask.Null, bool isAdditiveLayerAnimation = false, float transitionIn = .2f, float transitionOut = .2f, float animationSpeed = 1, Action<float, float> onAnimationUpdate = null, Action OnComplete = null, bool overridableByAnimator = false, params ActionData[] actions)
        {
            StartCoroutine(CrossfadeAsync(clip, clipInfo, false, false, transitionBack, mask, isAdditiveLayerAnimation, transitionIn, transitionOut, animationSpeed, onAnimationUpdate, OnComplete, overridableByAnimator, actions));
        }

        public void PlayLoopingAnimation(AnimationClip clip, AnimGraphClipInfo clipInfo = null, Mask mask = Mask.Null, bool isActAsAnimatorOutput = false, bool isAdditiveLayerAnimation = false, float transitionIn = .2f, float transitionOut = .2f, float animationSpeed = 1, Action<float, float> onAnimationUpdate = null, Action OnComplete = null)
        {
            StartCoroutine(CrossfadeAsync(clip, clipInfo, true, isActAsAnimatorOutput, false, mask, isAdditiveLayerAnimation, transitionIn, transitionOut, animationSpeed, onAnimationUpdate, OnComplete, overridableByAnimator: false));
        }

        public IEnumerator CrossfadeAsync(AnimationClip clip, AnimGraphClipInfo clipInfo = null, bool isLoopingAnimation = false, bool isActAsAnimatorOutput = false, bool transitionBack = true, Mask mask = Mask.Null, bool isAdditiveLayerAnimation = false, float transitionIn = .2f, float transitionOut = .2f, float animationSpeed = 1, Action<float, float> onAnimationUpdate = null, Action OnComplete = null, bool overridableByAnimator = false, params ActionData[] actions)
        {
            if (clipInfo != null)
            {
                if (clip == null) clip = clipInfo.clip;

                if (clipInfo.clip != null)
                {
                    transitionIn = clipInfo.TranistionInAndOut.x;
                    transitionOut = clipInfo.TranistionInAndOut.y;
                }
            }
            if (clip == null)
            {
                yield break;
            }

            var currentMixer = GetActionMixer(isLoopingAnimation);

            var clipPlayable = AnimationClipPlayable.Create(graph, clip);
            clipPlayable.SetSpeed(animationSpeed);
            var source = currentMixer.GetInput(0);
            currentMixer.DisconnectInput(0);



            var avatarMask = GetAvatarMask(mask);

            Playable clipMixer = default;
            if (avatarMask != null)
            {
                var layerMixer = AnimationLayerMixerPlayable.Create(graph, 2);
                if (isAdditiveLayerAnimation)
                    layerMixer.SetLayerAdditive(1, true);
                clipMixer = layerMixer;
            }
            else
            {
                clipMixer = AnimationMixerPlayable.Create(graph, 2);
            }

            clipMixer.ConnectInput(0, source, 0);
            clipMixer.ConnectInput(1, clipPlayable, 0);

            if (avatarMask != null)
            {
                ((AnimationLayerMixerPlayable)clipMixer).SetLayerMaskFromAvatarMask(1, avatarMask);
            }

            clipMixer.SetInputWeight(0, 1f);
            currentMixer.ConnectInput(0, clipMixer, 0);
            currentMixer.SetInputWeight(0, 1);
            currentMixer.SetInputWeight(1, 0);

            ResetTransitionPose();

            if (isLoopingAnimation)
                currentPlayableMixer = clipMixer;

            if (isActAsAnimatorOutput)
                animatorOutput = clipMixer;

            yield return UpdateWeights(clip, clipPlayable, clipMixer, currentMixer, clipInfo, isLoopingAnimation, isAdditiveLayerAnimation, transitionBack, avatarMask != null, transitionIn, transitionOut, animationSpeed, onAnimationUpdate, overridableByAnimator, actions);


            if (transitionBack)
            {
                if (clipMixer.IsValid())
                {
                    // Determine the correct input mixer:
                    // - If the current mixer is the loopActionMixer, use animatorMixer (to bypass loop).
                    // - Otherwise, continue using loopActionMixer for normal chaining.
                    var inputMixer = currentMixer.Equals(loopActionMixer) ? animatorMixer : loopActionMixer;

                    // Choose the correct playable to connect:
                    // - If this is an additive layer animation, connect to the base of the clipMixer (clipMixer.GetInput(0))
                    // - Otherwise, connect to the selected inputMixer
                    var m = isAdditiveLayerAnimation ? clipMixer.GetInput(0) : inputMixer;

                    // Start coroutine to connect the output of this clip to the next mixer in the chain
                    StartCoroutine(ConnectPlayables(clipMixer.GetOutput(0), m));

                }
            }
            else if (!isLoopingAnimation)
            {
                if (!currentAnimationIsAdditive)
                    StartCoroutine(ChangePreviousPlayableWeight(currentMixer, clipMixer));
            }

            OnComplete?.Invoke();
        }

        #endregion

        #region Override Controller

        public void UpdateOverrideController(AnimatorOverrideController overrideController, bool makeTransition = true)
        {
            StartCoroutine(UpdateOverrideControllerAsync(overrideController, makeTransition));
        }

        public IEnumerator UpdateOverrideControllerAsync(AnimatorOverrideController overrideController, bool makeTransition = true)
        {
            if (overrideController != null)
            {
                // Save Current Animation State (Time & Speed) 
                AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
                float normalizedTime = currentState.normalizedTime;
                float speed = animator.speed; // Save animation speed
                if (makeTransition)
                    SaveAnimatorParameters(); // Save existing animator parameters

                var anim = AnimatorControllerPlayable.Create(graph, overrideController);
                var clipMixer = AnimationMixerPlayable.Create(graph, 2);

                clipMixer.ConnectInput(0, anim, 0);
                clipMixer.SetInputWeight(0, 1);
                clipMixer.SetInputWeight(1, 0);

                int sourPort = animatorMixer.GetInput(1).IsValid() ? 1 : 0;
                int destPort = animatorMixer.GetInput(1).IsValid() ? 2 : 1;

                graph.Connect(clipMixer, 0, animatorMixer, destPort);

                animatorMixer.SetInputWeight(sourPort, 1);
                animatorMixer.SetInputWeight(destPort, 0);

                if (makeTransition)
                {
                    graph.Evaluate();
                    animator.Update(0);

                    //ResetTransitionPose();

                    RestoreAnimatorParameters(); // Restore saved parameters

                    // Resume Animation from Saved Pose
                    AnimatorStateInfo newState = animator.GetCurrentAnimatorStateInfo(0);
                    if (newState.fullPathHash == currentState.fullPathHash)
                    {
                        animator.Play(newState.fullPathHash, 0, normalizedTime % 1);
                    }

                    animator.speed = speed; // Restore animation speed


                    // Smooth Transition to New Animator
                    yield return MakeTransition(sourPort, destPort);
                }
                else
                {
                    animatorMixer.SetInputWeight(sourPort, 0);
                    animatorMixer.SetInputWeight(destPort, 1);
                }

                if (destPort == 2)
                {
                    animatorMixer.DisconnectInput(2);
                    var port1Mixer = animatorMixer.GetInput(1);
                    var overrideAnimator = animatorMixer.GetInput(1).GetInput(0);

                    animatorMixer.DisconnectInput(1);

                    graph.Connect(clipMixer, 0, animatorMixer, 1);
                    animatorMixer.SetInputWeight(0, 0);
                    animatorMixer.SetInputWeight(1, 1);

                    port1Mixer.Destroy();
                    overrideAnimator.Destroy();

                    ResetTransitionPose();
                }
                playableAnimator = anim;
            }

            else if (animatorMixer.GetInputWeight(0) < .5f)
            {
                int sourPort = 1;
                int destPort = 0;

                if (makeTransition)
                {
                    // 4. Smooth Transition to New Animator
                    yield return MakeTransition(sourPort, destPort);
                }
                else
                {
                    animatorMixer.SetInputWeight(sourPort, 0);
                    animatorMixer.SetInputWeight(destPort, 1);
                }

                var port1Mixer = animatorMixer.GetInput(1);
                var overrideAnimator = animatorMixer.GetInput(1).GetInput(0);

                animatorMixer.DisconnectInput(1);
                port1Mixer.Destroy();
                overrideAnimator.Destroy();
                playableAnimator = (AnimatorControllerPlayable)animatorMixer.GetInput(0).GetInput(0);

                ResetTransitionPose();
            }
        }

        IEnumerator MakeTransition(int sourPort, int destPort)
        {
            float transitionTime = 0.2f;
            float timer = 0;
            float weight = 0;

            while (timer < transitionTime)
            {
                //ResetTransitionPose();

                weight = Mathf.Lerp(1, 0, timer / transitionTime);
                if (timer >= transitionTime - DeltaTime) weight = 0;
                animatorMixer.SetInputWeight(sourPort, weight);
                animatorMixer.SetInputWeight(destPort, 1 - weight);
                timer += DeltaTime;
                yield return null;
            }
        }

        Dictionary<string, object> savedParameters = new Dictionary<string, object>();
        private void SaveAnimatorParameters()
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (animator.IsParameterControlledByCurve(parameter.nameHash)) continue;
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        savedParameters[parameter.name] = animator.GetFloat(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Int:
                        savedParameters[parameter.name] = animator.GetInteger(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        savedParameters[parameter.name] = animator.GetBool(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        savedParameters[parameter.name] = animator.GetBool(parameter.name);
                        break;
                }
            }
        }
        private void RestoreAnimatorParameters()
        {
            foreach (var parameter in savedParameters)
            {
                if (parameter.Value is float)
                {
                    animator.SetFloat(parameter.Key, (float)parameter.Value);
                }
                else if (parameter.Value is int)
                {
                    animator.SetInteger(parameter.Key, (int)parameter.Value);
                }
                else if (parameter.Value is bool)
                {
                    animator.SetBool(parameter.Key, (bool)parameter.Value);
                }
            }
        }

        #endregion

        #region Weight handle methods  

        IEnumerator ChangePreviousPlayableWeight(Playable currentMixer, Playable mixer)
        {
            yield return new WaitUntil(() => !currentMixer.GetInput(0).Equals(mixer));
            float transitionTime = .2f;
            yield return new WaitForSeconds(transitionTime);
            if (mixer.IsValid())
            {
                mixer.SetInputWeight(0, 1);
                mixer.SetInputWeight(1, 0);
            }
        }

        IEnumerator ConnectPlayables(Playable outputMixer, Playable inputMixer)
        {
            //yield break;
            if (!outputMixer.IsValid() || !inputMixer.IsValid() || outputMixer.GetInput(0).Equals(inputMixer) || !animator.enabled)
                yield break;

            // Collect all intermediate playables
            HashSet<Playable> intermediates = new HashSet<Playable>();
            CollectPlayables(outputMixer.GetInput(0), inputMixer, ref intermediates);

            //var mixer = outputMixer.GetInput(0);
            //var clip = mixer.GetInput(1);

            float duration = .2f;
            //float firstInputStartWeight = mixer.GetInputWeight(0);
            //float secondInputStartWeight = mixer.GetInputWeight(1);
            var prevWeightFirst = outputMixer.GetInputWeight(0);
            var prevWeightSecond = (outputMixer.Equals(nonLoopActionMixer) || outputMixer.Equals(loopActionMixer)) ? 1 : outputMixer.GetInputWeight(1);
            outputMixer.DisconnectInput(0);
            inputMixer.GetOutput(0).DisconnectInput(0);
            outputMixer.ConnectInput(0, inputMixer, 0);
            outputMixer.SetInputWeight(0, prevWeightFirst);

            graph.Evaluate();
            yield return null;
            ResetTransitionPose();
            //animator.Update(0);

            float timer = 0;
            float weight = 0;

            //while (timer < duration)
            //{
            //    weight = Mathf.Lerp(prevWeightFirst, 1, timer / duration);
            //    if (timer >= duration - DeltaTime) weight = 0;

            //    if (1 - weight > prevWeightFirst)
            //        outputMixer.SetInputWeight(0, 1 - weight);
            //    outputMixer.SetInputWeight(1, weight);

            //    timer += DeltaTime;
            //    yield return null;
            //}

            outputMixer.SetInputWeight(0, prevWeightFirst);
            //outputMixer.SetInputWeight(1, 0);

            // Destroy all collected playables
            foreach (var intermediate in intermediates)
            {
                if (intermediate.IsValid())
                    intermediate.Destroy();
            }
            //outputMixer.GetInput(0).Destroy();

            //if (mixer.IsValid())
            //    mixer.Destroy();
            //if (clip.IsValid())
            //    clip.Destroy();
            yield break;
        }
        void CollectPlayables(Playable current, Playable target, ref HashSet<Playable> collected)
        {
            if (!current.IsValid() || collected.Contains(current) || current.Equals(target))
                return;

            collected.Add(current);

            int inputCount = current.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                CollectPlayables(current.GetInput(i), target, ref collected);
            }
        }
        IEnumerator LoopingAnimationTransitionback()
        {
            float timer = 0;
            float weight = 0;
            float transitionTime = .2f;
            var mixer = loopActionMixer.GetInput(0);
            if (animatorMixer.Equals(mixer) || !mixer.Equals(currentPlayableMixer))
            {
                yield break;
            }
            var prevMixer = mixer.GetInput(0);
            float firstInputStartWeight = mixer.GetInputWeight(0);
            while (timer < transitionTime)
            {
                //ResetTransitionPose();

                weight = Mathf.Lerp(firstInputStartWeight, 0, timer / transitionTime);
                if (timer >= transitionTime - DeltaTime) weight = 0;

                if (mixer.IsValid())
                {
                    if (1 - weight > mixer.GetInputWeight(0))
                        mixer.SetInputWeight(0, 1 - weight);
                    mixer.SetInputWeight(1, weight);
                }
                timer += DeltaTime;
                yield return null;
            }

            if (mixer.IsValid())
            {
                var inputPlayable = mixer.GetInput(0);
                var outputPlayable = mixer.GetOutput(0);
                StartCoroutine(ConnectPlayables(outputPlayable, inputPlayable));
            }
            //yield return new WaitUntil(() => loopActionMixer.GetInput(0).Equals(mixer) || !mixer.IsValid());

            //if (mixer.IsValid())
            //{
            //    mixer.SetInputWeight(0, 1);
            //    mixer.SetInputWeight(1, 0);
            //    loopActionMixer.DisconnectInput(0);
            //    mixer.DisconnectInput(0);
            //    loopActionMixer.ConnectInput(0, prevMixer, 0);
            //    loopActionMixer.SetInputWeight(0, 1);
            //    loopActionMixer.SetInputWeight(1, 0);
            //    if(mixer.GetInput(1).IsValid())
            //        mixer.GetInput(1).Destroy();
            //    mixer.Destroy();
            //    ResetTransitionPose();
            //}
            if (!loopActionMixer.GetInput(0).Equals(animatorMixer))
                currentPlayableMixer = loopActionMixer.GetInput(0);
        }

        IEnumerator UpdateWeights(AnimationClip clip, Playable clipPlayable, Playable mixer, Playable currentMixer, AnimGraphClipInfo clipInfo = null, bool isLoopingAnimation = false, bool isAdditiveLayerAnimation = false, bool transitionBack = true, bool isLayerMixer = false, float transitionIn = .2f,
         float transitionOut = .2f, float animationSpeed = 1, Action<float, float> onAnimationUpdate = null, bool overridableByAnimator = false, params ActionData[] actions)
        {
            if (clipInfo != null)
                clip = clipInfo.clip ?? clip; // Assign clip if clipInfo has a valid clip

            if (clip == null)
                yield break; // Exit coroutine if no clip is available


            float timer = 0f;
            float normalizedTimer = 0f;
            float weight = 0f;

            float clipLength = clip.length;

            bool isSmallClip = clipLength < .5f;

            if (!isLoopingAnimation && isSmallClip) clipLength = .5f;

            //if (isLoopingAnimation) isInLoopingAnimation = true;

            // Ensure transitions don't exceed clip length
            transitionIn = transitionIn > clipLength ? clipLength * 0.3f : transitionIn;
            transitionOut = transitionOut > clipLength ? clipLength * 0.3f : transitionOut;

            // Initialize clipInfo if null
            clipInfo ??= new AnimGraphClipInfo { clip = clip };

            // Set initial clip state info
            currentClipStateInfo.currentClipInfo = clipInfo;
            currentClipStateInfo.normalizedTime = normalizedTimer;
            currentClipStateInfo.clipLength = clipLength;
            currentClipStateInfo.timer = timer;
            currentClipStateInfo.currentMixer = mixer;

            currentAnimationIsAdditive = isAdditiveLayerAnimation;

            // Generate a unique identifier for the animation instance
            string uniqueId = Guid.NewGuid().ToString();

            // Handle time scaling if using a global time scale
            if (clipInfo.customAnimationSpeed && clipInfo.useAsGlobalTimeScale)
            {
                TimeScaleOwner = TimeScaleOwner.Key != "" ? new(uniqueId, TimeScaleOwner.Value) : new(uniqueId, Time.timeScale);
            }

            float currentTimescale = TimeScaleOwner.Value;

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            // Animation loop with transition in/out handling
            while (timer <= clipLength)
            {

                // Apply custom animation speed settings
                if (clipInfo.customAnimationSpeed)
                {
                    if (clipInfo.useAsGlobalTimeScale && TimeScaleOwner.Key == uniqueId)
                    {
                        Time.timeScale = currentTimescale * Mathf.Max(clipInfo.speedModifier.GetValue(normalizedTimer), 0.01f);
                        animationSpeed = 1;
                    }
                    else
                    {
                        animationSpeed = Mathf.Max(clipInfo.speedModifier.GetValue(normalizedTimer), 0.01f);
                        if (clipPlayable.IsValid())
                            clipPlayable.SetSpeed(animationSpeed);
                    }
                }

                // Invoke animation events at the correct time
                foreach (var item in clipInfo.events)
                {
                    if (item.normalizedTime >= normalizedTimer && item.normalizedTime <= normalizedTimer + DeltaTime * animationSpeed)
                    {
                        item.InvokeCustomAnimationEvent(gameObject);
                        item.unityEvent.Invoke();
                    }
                }

                if (timer <= transitionIn)
                {
                    weight = Mathf.Lerp(0, 1, timer / transitionIn);
                    if (timer >= transitionIn - DeltaTime) weight = 1; // Ensure it reaches exactly 1
                }
                else if (isLoopingAnimation)
                {
                    yield break;
                }
                else if ((transitionBack) && timer > clipLength - transitionOut)
                {
                    weight = Mathf.Lerp(1, 0, (timer - (clipLength - transitionOut)) / transitionOut);
                    if (timer >= clipLength) weight = 0; // Ensure it reaches exactly 0
                }
                if ((animator.isMatchingTarget || animator.IsInTransition(0)) && overridableByAnimator)
                {
                    if (mixer.IsValid())
                    {
                        mixer.SetInputWeight(0, 1);
                        mixer.SetInputWeight(1, 0);
                    }
                    yield break;
                }

                if (mixer.IsValid())
                {
                    // Don change clip weight if the animation looping or additive
                    if (!isLayerMixer && !isAdditiveLayerAnimation)
                        mixer.SetInputWeight(0, 1 - weight);
                    mixer.SetInputWeight(1, weight);
                }

                // Check for animation override conditions
                if (transitionBack && (!currentMixer.GetInput(0).Equals(mixer)) && !currentAnimationIsAdditive)
                {
                    var waitTIme = currentClipInfo != null ? currentClipInfo.TranistionInAndOut.x : .2f;
                    yield return new WaitForSeconds(waitTIme);
                    if (mixer.IsValid())
                    {
                        mixer.SetInputWeight(0, 1);
                        mixer.SetInputWeight(1, 0);
                    }
                    yield break;
                }
                else
                {
                    timer += DeltaTime * animationSpeed;
                }
                // Update normalized time
                normalizedTimer = timer / clipLength;
                currentClipStateInfo.normalizedTime = normalizedTimer;


                foreach (var action in actions)
                {
                    if (!action.actionInvoked && (normalizedTimer >= action.normalizeTime))
                    {
                        action.action?.Invoke();
                        action.actionInvoked = true;
                    }
                }


                currentClipStateInfo.timer = timer;
                currentClipStateInfo.deltaTime = DeltaTime * animationSpeed;
                onAnimationUpdate?.Invoke(normalizedTimer, timer);

                yield return null;
            }
            foreach (var item in clipInfo.onEndAnimation)
            {
                item.InvokeCustomAnimationEvent(this.gameObject);
                item.unityEvent.Invoke();
            }
            currentClipStateInfo.currentClipInfo = null;

            if (TimeScaleOwner.Key == uniqueId && clipInfo.customAnimationSpeed && clipInfo.useAsGlobalTimeScale)
            {
                Time.timeScale = currentTimescale;
                TimeScaleOwner = new KeyValuePair<string, float>("", Time.timeScale);
            }
        }

        #endregion

        #region Utility Methods

        public void SetUpdateMode(UpdateMode updateMode)
        {
            this.updateMode = updateMode;
        }

        AvatarMask GetAvatarMask(Mask mask)
        {
            if (mask == Mask.Null) return null;

            switch (mask)
            {
                case Mask.Arm:
                    return armMask;
                case Mask.UpperBody:
                    return upperBodyMask;
                case Mask.RightHand:
                    return rightHandMask;
                case Mask.LeftHand:
                    return leftHandMask;
                case Mask.UpperBodyWithRoot:
                    return upperBodyWithRoot;
                case Mask.UpperBodyWithoutArm:
                    return upperBodyWithoutArm;
                case Mask.RightArm:
                    return rightArm;
            }
            return null;
        }
        Playable GetActionMixer(bool isLoopingAnimation)
        {
            if (isLoopingAnimation)
                return loopActionMixer;
            else
                return nonLoopActionMixer;
        }
        public void StopLoopingAnimations(bool removeAll)
        {
            if (removeAll)
            {
                animatorOutput = defaultAnimatorOutput;
                StartCoroutine(ConnectPlayables(loopActionMixer, animatorMixer));
            }
            else
            {
                StartCoroutine(LoopingAnimationTransitionback());
            }
        }
        public void StopCurrentNonLoopingAnimation()
        {
            StartCoroutine(ConnectPlayables(nonLoopActionMixer, loopActionMixer));
        }



        public void ResetAnimationGraph()
        {
            CreateGraph();
        }

        public List<TransitionData> transitionDatas = new List<TransitionData>();
        public struct TransitionData
        {
            public int currentShortNameHash;
            public float currentNormalizedTime;
            public int NextShortNameHash;
            public float NextNormalizedTime;
            public float tranistionDuration;
            public float normalizedTransition;
            public DurationUnit durationUnit;
        }

        void ResetTransitionPose()
        {
            if (transitionDatas.Count < 1) return;
            animator.Update(0);
            SaveTransitionData();
            var oldPos = transform.position;
            var oldRot = transform.rotation;
            graph.Evaluate();
            animator.CrossFade(transitionDatas.First().currentShortNameHash, 0, 0, transitionDatas.First().currentNormalizedTime);
            animator.Update(0);
            graph.Evaluate();

            foreach (var data in transitionDatas)
            {
                animator.CrossFade(data.NextShortNameHash, data.tranistionDuration, 0, data.NextNormalizedTime, data.normalizedTransition);
                animator.Update(0);
                graph.Evaluate();
            }
            transform.position = oldPos;
            transform.rotation = oldRot;
            transitionDatas.Clear();
        }

        void SaveTransitionData()
        {
            if (animator.IsInTransition(0))
            {
                var newTansition = new TransitionData()
                {
                    currentShortNameHash = animator.GetCurrentAnimatorStateInfo(0).shortNameHash,
                    NextShortNameHash = animator.GetNextAnimatorStateInfo(0).shortNameHash,
                    currentNormalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
                    NextNormalizedTime = animator.GetNextAnimatorStateInfo(0).normalizedTime,
                    tranistionDuration = animator.GetAnimatorTransitionInfo(0).duration,
                    normalizedTransition = animator.GetAnimatorTransitionInfo(0).normalizedTime,
                    durationUnit = animator.GetAnimatorTransitionInfo(0).durationUnit
                };
                if (transitionDatas.Count == 0 || (transitionDatas.Last().currentShortNameHash != newTansition.currentShortNameHash || transitionDatas.Last().NextShortNameHash != newTansition.NextShortNameHash))
                    transitionDatas.Add(newTansition);
                else
                {
                    var last = transitionDatas.Last();
                    last.currentNormalizedTime = newTansition.currentNormalizedTime;
                    last.NextNormalizedTime = newTansition.NextNormalizedTime;
                    last.tranistionDuration = newTansition.tranistionDuration;
                    last.normalizedTransition = newTansition.normalizedTransition;
                    last.durationUnit = newTansition.durationUnit;
                    transitionDatas[transitionDatas.Count - 1] = last;
                }
            }
            else if (transitionDatas.Count > 0)
            {
                transitionDatas.Clear();
            }
        }


        #endregion
    }
    public class ActionData
    {
        public float normalizeTime = 1;
        public Action action;
        public bool actionInvoked = false;
    }
}