using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace FS_Util
{
    public enum LogicOperator
    {
        And,
        Or
    }


    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string ConditionField1 { get; }
        public object ExpectedValue1 { get; }
        public string ConditionField2 { get; }
        public object ExpectedValue2 { get; }
        public ComparisonType Comparison { get; }
        public LogicOperator LogicOp { get; }
        public bool HasTwoConditions { get; }

        public ShowIfAttribute(string conditionField, object expectedValue, ComparisonType comparison = ComparisonType.Equals)
        {
            ConditionField1 = conditionField;
            ExpectedValue1 = expectedValue;
            Comparison = comparison;
        }
        public ShowIfAttribute(LogicOperator logicOperator, string conditionField1, object expectedValue1, string conditionField2, object expectedValue2)
        {
            ConditionField1 = conditionField1;
            ExpectedValue1 = expectedValue1;
            ConditionField2 = conditionField2;
            ExpectedValue2 = expectedValue2;
            LogicOp = logicOperator;
            Comparison = ComparisonType.Equals;
            HasTwoConditions = true;
        }
    }

    public enum ComparisonType
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ShouldShow(property) ? EditorGUI.GetPropertyHeight(property, label, true) : 0;
        }

        private bool ShouldShow(SerializedProperty property)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;

            if (!showIf.HasTwoConditions)
            {
                return EvaluateCondition(property, showIf.ConditionField1, showIf.ExpectedValue1, showIf.Comparison);
            }

            bool condition1 = EvaluateCondition(property, showIf.ConditionField1, showIf.ExpectedValue1, showIf.Comparison);
            bool condition2 = EvaluateCondition(property, showIf.ConditionField2, showIf.ExpectedValue2, showIf.Comparison);

            return showIf.LogicOp == LogicOperator.And ? condition1 && condition2 : condition1 || condition2;
        }

        private bool EvaluateCondition(SerializedProperty property, string conditionField, object expectedValue, ComparisonType comparison)
        {
            SerializedProperty conditionProperty = property.serializedObject.FindProperty(conditionField);

            if (conditionProperty == null)
                return false;

            object conditionValue = GetPropertyValue(conditionProperty);
            if (conditionValue == null)
                return false;

            return CompareValues(conditionValue, expectedValue, comparison);
        }

        private object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean: return property.boolValue;
                case SerializedPropertyType.Enum: return property.enumValueIndex;
                case SerializedPropertyType.Integer: return property.intValue;
                case SerializedPropertyType.String: return property.stringValue;
                case SerializedPropertyType.Float: return property.floatValue;
                default: return null;
            }
        }

        private bool CompareValues(object a, object b, ComparisonType comparison)
        {
            try
            {
                double aVal = Convert.ToDouble(a);
                double bVal = Convert.ToDouble(b);

                return comparison switch
                {
                    ComparisonType.Equals => aVal == bVal,
                    ComparisonType.NotEquals => aVal != bVal,
                    ComparisonType.GreaterThan => aVal > bVal,
                    ComparisonType.LessThan => aVal < bVal,
                    ComparisonType.GreaterThanOrEqual => aVal >= bVal,
                    ComparisonType.LessThanOrEqual => aVal <= bVal,
                    _ => false,
                };
            }
            catch
            {
                return a.Equals(b);
            }
        }
    }
#endif
}