/*---------------------------
| File: ConditionTests.cs
| Author: Chandler Mays
----------------------------*/
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
//---------------------------------
using PolyQuest.Tools;

namespace PolyQuest.Tests
{
    public class ConditionTests
    {
        private sealed class StubChecker : IConditionChecker
        {
            private readonly Func<ConditionType, string[], bool?> m_evaluate;

            public int Calls { get; private set; }
            public ConditionType LastCondition { get; private set; }
            public string[] LastParameters { get; private set; }

            public StubChecker(Func<ConditionType, string[], bool?> evaluate) => m_evaluate = evaluate;

            public static StubChecker Always(bool? result) => new StubChecker((_, _) => result);

            public bool? Evaluate(ConditionType condition, string[] parameters)
            {
                ++Calls;
                LastCondition = condition;
                LastParameters = parameters;
                return m_evaluate(condition, parameters);
            }
        }

        /* --- Fixture builders (JSON -> JsonUtility) --- */

        private static string CondJson(ConditionType type, bool negate, params string[] parameters)
        {
            string quoted = string.Join(",", parameters.Select(p => $"\"{p}\""));
            return $"{{\"m_condition\":{(int)type},\"m_parameters\":[{quoted}],\"m_negate\":{(negate ? "true" : "false")}}}";
        }

        private static string OrJson(params string[] conditions) => $"{{\"m_OR\":[{string.Join(",", conditions)}]}}";

        private static Conjunction.Condition Cond(ConditionType type, bool negate = false, params string[] parameters)
            => JsonUtility.FromJson<Conjunction.Condition>(CondJson(type, negate, parameters));

        private static Conjunction.Disjunction Or(params string[] conditions)
            => JsonUtility.FromJson<Conjunction.Disjunction>(OrJson(conditions));

        private static Conjunction And(params string[] disjunctions)
            => JsonUtility.FromJson<Conjunction>($"{{\"m_AND\":[{string.Join(",", disjunctions)}]}}");

        // Answers by condition type: kHasQuest -> true, kHasItem -> false, anything else -> null (not handled)
        private static StubChecker TypeKeyed() => new StubChecker((type, _) => type switch
        {
            ConditionType.kHasQuest => true,
            ConditionType.kHasItem => false,
            _ => null
        });

        private static IConditionChecker[] Checkers(params IConditionChecker[] checkers) => checkers;

        /* ============================== Condition ============================== */

        [TestCase(true, false, true)]
        [TestCase(false, false, false)]
        [TestCase(true, true, false)]
        [TestCase(false, true, true)]
        [Description("A condition returns the checker's answer, inverted when m_negate is set.")]
        public void Condition_TruthTable_WithNegate(bool checkerResult, bool negate, bool expected)
        {
            var condition = Cond(ConditionType.kHasQuest, negate);

            Assert.That(condition.Check(Checkers(StubChecker.Always(checkerResult))), Is.EqualTo(expected));
        }

        [TestCase(false)]
        [TestCase(true)]
        [Description("When every checker returns null (unhandled), the condition passes, even when negated.")]
        public void Condition_NoCheckerHandlesIt_Passes(bool negate)
        {
            var condition = Cond(ConditionType.kHasSpokenTo, negate);

            Assert.That(condition.Check(Checkers(StubChecker.Always(null))), Is.True);
        }

        [Test]
        [Description("A condition checked against an empty checker list passes.")]
        public void Condition_NoCheckers_Passes()
        {
            Assert.That(Cond(ConditionType.kHasQuest).Check(Checkers()), Is.True);
        }

        [Test]
        [Description("Checkers receive the condition's type and its parameter array unchanged.")]
        public void Condition_PassesTypeAndParametersToCheckers()
        {
            var checker = StubChecker.Always(true);
            var condition = Cond(ConditionType.kHasItems, false, "sword", "2");

            condition.Check(Checkers(checker));

            Assert.That(checker.LastCondition, Is.EqualTo(ConditionType.kHasItems));
            Assert.That(checker.LastParameters, Is.EqualTo(new[] { "sword", "2" }));
        }

        [Test]
        [Description("Null answers are skipped; any checker that answers false fails the condition, even if another answers true.")]
        public void Condition_AcrossCheckers_IgnoresNulls_AndAnyMismatchFails()
        {
            var condition = Cond(ConditionType.kHasQuest);

            Assert.That(condition.Check(Checkers(StubChecker.Always(null), StubChecker.Always(true))), Is.True);
            Assert.That(condition.Check(Checkers(StubChecker.Always(null), StubChecker.Always(false))), Is.False);
            Assert.That(condition.Check(Checkers(StubChecker.Always(true), StubChecker.Always(false))), Is.False);
        }

        [Test]
        [Description("A condition short-circuits: checkers after the first failing one are never called.")]
        public void Condition_StopsAtFirstFailingChecker()
        {
            var failing = StubChecker.Always(false);
            var later = StubChecker.Always(true);

            Cond(ConditionType.kHasQuest).Check(Checkers(failing, later));

            Assert.That(failing.Calls, Is.EqualTo(1));
            Assert.That(later.Calls, Is.EqualTo(0));
        }

        /* ============================= Disjunction ============================= */

        [Test]
        [Description("A disjunction (OR) is true when at least one of its conditions is true.")]
        public void Disjunction_IsTrue_WhenAnyConditionIsTrue()
        {
            var disjunction = Or(CondJson(ConditionType.kHasItem, false), CondJson(ConditionType.kHasQuest, false));

            Assert.That(disjunction.Check(Checkers(TypeKeyed())), Is.True);
        }

        [Test]
        [Description("A disjunction is false when none of its conditions is true.")]
        public void Disjunction_IsFalse_WhenNoConditionIsTrue()
        {
            var disjunction = Or(CondJson(ConditionType.kHasItem, false), CondJson(ConditionType.kHasItem, false, "x"));

            Assert.That(disjunction.Check(Checkers(TypeKeyed())), Is.False);
        }

        [Test]
        [Description("A disjunction with no conditions is false.")]
        public void Disjunction_Empty_IsFalse()
        {
            Assert.That(Or().Check(Checkers(TypeKeyed())), Is.False);
        }

        [Test]
        [Description("A disjunction short-circuits: it stops evaluating at the first true condition.")]
        public void Disjunction_StopsAtFirstTrueCondition()
        {
            var checker = TypeKeyed();
            var disjunction = Or(CondJson(ConditionType.kHasQuest, false), CondJson(ConditionType.kHasItem, false));

            disjunction.Check(Checkers(checker));

            Assert.That(checker.Calls, Is.EqualTo(1));
        }

        /* ============================== Conjunction ============================== */

        [Test]
        [Description("A conjunction (AND) is true when every one of its disjunctions is true.")]
        public void Conjunction_IsTrue_WhenEveryDisjunctionIsTrue()
        {
            var conjunction = And(
                OrJson(CondJson(ConditionType.kHasQuest, false)),
                OrJson(CondJson(ConditionType.kHasItem, false), CondJson(ConditionType.kHasQuest, false)));

            Assert.That(conjunction.Check(Checkers(TypeKeyed())), Is.True);
        }

        [Test]
        [Description("A conjunction is false when any one of its disjunctions is false.")]
        public void Conjunction_IsFalse_WhenAnyDisjunctionIsFalse()
        {
            var conjunction = And(
                OrJson(CondJson(ConditionType.kHasQuest, false)),
                OrJson(CondJson(ConditionType.kHasItem, false)));

            Assert.That(conjunction.Check(Checkers(TypeKeyed())), Is.False);
        }

        [Test]
        [Description("Negating a failing condition inside a conjunction makes the whole conjunction pass.")]
        public void Conjunction_Negation_FlipsOneCondition()
        {
            var conjunction = And(
                OrJson(CondJson(ConditionType.kHasQuest, false)),
                OrJson(CondJson(ConditionType.kHasItem, true)));

            Assert.That(conjunction.Check(Checkers(TypeKeyed())), Is.True);
        }

        [Test]
        [Description("A conjunction short-circuits: it stops evaluating at the first false disjunction.")]
        public void Conjunction_StopsAtFirstFalseDisjunction()
        {
            var checker = TypeKeyed();
            var conjunction = And(
                OrJson(CondJson(ConditionType.kHasItem, false)),
                OrJson(CondJson(ConditionType.kHasQuest, false)));

            conjunction.Check(Checkers(checker));

            Assert.That(checker.Calls, Is.EqualTo(1));
        }

        [Test]
        [Description("A conjunction with no disjunctions is true.")]
        public void Conjunction_WithNoDisjunctions_IsTrue()
        {
            Assert.That(And().Check(Checkers(TypeKeyed())), Is.True);
        }

        [Test]
        [Description("A conjunction containing an empty disjunction is false, because the empty disjunction is false.")]
        public void Conjunction_ContainingAnEmptyDisjunction_IsFalse()
        {
            Assert.That(And(OrJson()).Check(Checkers(TypeKeyed())), Is.False);
        }

        [Test]
        [Description("A conjunction whose conditions no checker handles passes.")]
        public void Conjunction_WithUnhandledConditions_Passes()
        {
            var conjunction = And(OrJson(CondJson(ConditionType.kHasLevel, false, "10")));

            Assert.That(conjunction.Check(Checkers(TypeKeyed())), Is.True);
        }
    }
}