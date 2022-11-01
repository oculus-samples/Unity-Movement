// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor.Events;
#endif

namespace Oculus.Movement.Validation
{
    /// <summary>
    /// For managing Unit Tests in Unity at runtime, for sample scenes and debugging/diagnostics.
    /// <see cref="LayerAndVulkanValidation"/> is an example implementation of this base class.
    /// </summary>
    public class RuntimeUnitValidation : MonoBehaviour
    {
        /// <summary>
        /// List of TestCases, which are functions that call a given result bool callback.
        /// </summary>
        [Tooltip(RuntimeUnitValidationTooltips.TestCases)]
        [SerializeField]
        protected List<TestCase> _testCases = new List<TestCase>();

        /// <summary>
        /// All test cases, where each test case accepts a <code>void method(bool)</code> delegate
        /// to invoke once a test is complete with a success/failure result known.
        /// </summary>
        public List<TestCase> TestCases { get => _testCases; set => _testCases = value; }

        /// <summary>
        /// When the result of a test is known, the given callback will be called.
        /// If the test algorithm fails, the callback may never be called.
        /// </summary>
        [Serializable]
        public class TestEvent : UnityEvent<TestResultHandler> { }

        /// <summary>
        /// Template for a function to be called when the results of a test are known.
        /// </summary>
        /// <param name="testResult">if the test passed (true) or failed (false).</param>
        public delegate void TestResultHandler(bool testResult);

        [Serializable]
        public class TestCase
        {
            /// <summary>
            /// Metadata describing the test.
            /// </summary>
            [Tooltip(RuntimeUnitValidationTooltips.TestCase.Name)]
            public string Name;

            /// <summary>
            /// Function that accepts a bool callback, giving it the test result.
            /// </summary>
            [Tooltip(RuntimeUnitValidationTooltips.TestCase.Test)]
            public TestEvent Test = new TestEvent();

            /// <summary>
            /// Unity Editor can insert a response here to a true case from the test.
            /// </summary>
            [Tooltip(RuntimeUnitValidationTooltips.TestCase.OnTrue)]
            public UnityEvent OnTrue = new UnityEvent();

            /// <summary>
            /// Unity Editor can insert a response here to a false case from the test.
            /// </summary>
            [Tooltip(RuntimeUnitValidationTooltips.TestCase.OnFalse)]
            public UnityEvent OnFalse = new UnityEvent();

            /// <summary>
            /// Creates a validation test unit.
            /// </summary>
            /// <param name="target">Object with a <see cref="TestResultHandler"/>.</param>
            /// <param name="testName">Name of the <see cref="TestResultHandler"/>.</param>
            public TestCase(object target, string testName)
            {
#if UNITY_EDITOR
                Name = testName;
                BindDelegate(Test, target, testName);
                Test.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
#endif
            }

            /// <summary>
            /// Runs the test, then calls <see cref="OnTrue"/> or <see cref="OnFalse"/>.
            /// </summary>
            /// <param name="action">An additional result callback to use, null is fine.</param>
            public void Evaluate(TestResultHandler action)
            {
                if (action == null)
                {
                    action = OnResultKnown;
                }
                else
                {
                    action += OnResultKnown;
                }
                Test.Invoke(action);
            }

            /// <summary>
            /// Called when the result of the test is known.
            /// </summary>
            /// <param name="result">Result of the test.</param>
            protected void OnResultKnown(bool result)
            {
                if (result)
                {
                    OnTrue.Invoke();
                    return;
                }
                OnFalse.Invoke();
            }
        }

        /// <summary>
        /// Automatically run this test when this object starts in the scene.
        /// </summary>
        public virtual void Start()
        {
            Test();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Reset methods are for Unity Editor data population and should not exist at runtime.
        /// </summary>
        public virtual void Reset()
        {
        }

        /// <summary>
        /// Helper function for binding <see cref="UnityEvent"/>s with C# scripts.
        /// Calling this method should be limited to within the editor only since the C# reflection
        /// used causes persistent performance penalties at runtime.
        /// </summary>
        /// <param name="event">A UnityEvent, editable by the Unity Editor at edit time.</param>
        /// <param name="target">An object with a <code>void method()</code>.</param>
        /// <param name="methodName"><code>nameof(method)</code>uses reflection.</param>
        public static void BindSimpleDelegate(UnityEvent @event, object target, string methodName)
        {
            MethodInfo method =
                UnityEvent.GetValidMethodInfo(target, methodName, Array.Empty<Type>());
            UnityAction action =
                Delegate.CreateDelegate(typeof(UnityAction), target, method, false) as UnityAction;
            UnityEventTools.AddVoidPersistentListener(@event, action);
        }

        /// <summary>
        /// Helper function for binding <see cref="UnityEvent"/>&lt;bool&gt;s with C# scripts.
        /// Calling this method should be limited to within the editor only since the C# reflection
        /// used causes persistent performance penalties at runtime.
        /// </summary>
        /// <param name="event">A UnityEvent, editable by the Unity Editor at edit time.</param>
        /// <param name="target">An object with a <code>void method(bool)</code>.</param>
        /// <param name="methodName"><code>nameof(method)</code>uses reflection.</param>
        /// <param name="argument">Whether to pass true or false on the event.</param>
        public static void BindDelegateWithBool
            (UnityEvent @event, object target, string methodName, bool argument)
        {
            MethodInfo method = FindMethod(target, methodName, typeof(bool));
            Type delegateType = typeof(UnityAction<bool>);
            UnityAction<bool> action =
                Delegate.CreateDelegate(delegateType, target, method, false) as UnityAction<bool>;
            UnityEventTools.AddBoolPersistentListener(@event, action, argument);
        }

        /// <summary>
        /// Helper function for binding <see cref="UnityEvent"/>&lt;T&gt;s with C# scripts.
        /// Calling this method should be limited to within the editor only since the C# reflection
        /// used causes persistent performance penalties at runtime.
        /// </summary>
        /// <param name="event">A UnityEvent, editable by the Unity Editor at edit time.</param>
        /// <param name="target">An object with a <code>void method(T)</code>.</param>
        /// <param name="methodName"><code>nameof(method)</code>uses reflection.</param>
        /// <typeparam name="T"></typeparam>
        public static void BindDelegate<T>(UnityEvent<T> @event, object target, string methodName)
        {
            MethodInfo method = FindMethod(target, methodName, typeof(T));
            Type delegateType = typeof(UnityAction<T>);
            UnityAction<T> action =
                Delegate.CreateDelegate(delegateType, target, method, false) as UnityAction<T>;
            UnityEventTools.AddPersistentListener(@event, action);
        }

        private static MethodInfo FindMethod(object target, string methodName, Type argType)
        {
            MethodInfo method =
                UnityEvent.GetValidMethodInfo(target, methodName, new Type[] { argType });
            if (method == null)
            {
                throw new Exception($"missing {methodName}({argType}) in {target.GetType()}");
            }
            return method;
        }
#endif

        /// <summary>
        /// Calls <see cref="TestCase"/>s, based on <see cref="TestCase.Test"/> evaluation, calls
        /// <see cref="TestCase.OnTrue"/> or <see cref="TestCase.OnFalse"/>.
        /// </summary>
        public virtual void Test()
        {
            _testCases.ForEach(ExecuteTest);
        }

        private void ExecuteTest(TestCase test)
        {
            TestResultHandler extraResultCallback = null;
            if (!Application.isPlaying)
            {
                extraResultCallback = (result) => { Debug.Log($"{test.Name} : {result}"); };
            }
            test.Evaluate(extraResultCallback);
        }
    }
}
