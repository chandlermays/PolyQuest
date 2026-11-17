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
        /*-------------------------------------------------------------------------------------------------
        | --- StubChecker: A test checker that answers with a supplied function and records each call --- |
        -------------------------------------------------------------------------------------------------*/
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


        /*--------------------------
        | --- Fixture Builders --- |
        --------------------------*/
        /*--------------------------------------------------------------------------------------------
        | --- CondJson: Builds the JSON for one condition with its type, negation and parameters --- |
        --------------------------------------------------------------------------------------------*/
        private static string CondJson(ConditionType type, bool negate, params string[] parameters)
        {
            string quoted = string.Join(",", parameters.Select(p => $"\"{p}\""));
            return $"{{\"m_condition\":{(int)type},\"m_parameters\":[{quoted}],\"m_negate\":{(negate ? "true" : "false")}}}";
        }

        /*---------------------------------------------------------------------------
        | --- OrJson: Builds the JSON for a disjunction of the given conditions --- |
        ---------------------------------------------------------------------------*/
        private static string OrJson(params string[] conditions) => $"{{\"m_OR\":[{string.Join(",", conditions)}]}}";

        /*-------------------------------------------------------------
        | --- Cond: Deserializes a single condition from its JSON --- |
        -------------------------------------------------------------*/
        private static Conjunction.Condition Cond(ConditionType type, bool negate = false, params string[] parameters)
            => JsonUtility.FromJson<Conjunction.Condition>(CondJson(type, negate, parameters));

        /*------------------------------------------------------------------
        | --- Or: Deserializes a disjunction from its conditions' JSON --- |
        ------------------------------------------------------------------*/
        private static Conjunction.Disjunction Or(params string[] conditions)
            => JsonUtility.FromJson<Conjunction.Disjunction>(OrJson(conditions));

        /*---------------------------------------------------------------------
        | --- And: Deserializes a conjunction from its disjunctions' JSON --- |
        ---------------------------------------------------------------------*/
        private static Conjunction And(params string[] disjunctions)
            => JsonUtility.FromJson<Conjunction>($"{{\"m_AND\":[{string.Join(",", disjunctions)}]}}");

        /*-----------------------------------------------------------------------------------------------------------------------
        | --- TypeKeyed: A checker that answers kHasQuest true, kHasItem false, and leaves every other condition unanswered --- |
        -----------------------------------------------------------------------------------------------------------------------*/
        private static StubChecker TypeKeyed() => new StubChecker((type, _) => type switch
        {
            ConditionType.kHasQuest => true,
            ConditionType.kHasItem => false,
            _ => null
        });

        /*--------------------------------------------------------------------
        | --- Checkers: Packs checkers into the array a Check call takes --- |
        --------------------------------------------------------------------*/
        private static IConditionChecker[] Checkers(params IConditionChecker[] checkers) => checkers;


        /*-------------------
        | --- Condition --- |
        -------------------*/
        /*------------------------------------------------------------------------------------------------------------------
        | --- Condition_TruthTable_WithNegate: A condition returns the checker's answer, inverted when m_negate is set --- |
        ------------------------------------------------------------------------------------------------------------------*/
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

        /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- Condition_NoCheckerAnswersIt_Fails: A condition no checker answers (e.g. kHasLevel, which no system evaluates) fails, whether or not it is negated: negation flips an answer, not the absence of one --- |
        --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [TestCase(false)]
        [TestCase(true)]
        [Description("A condition no checker answers (e.g. kHasLevel, which no system evaluates) fails, whether or not it is negated: negation flips an answer, not the absence of one.")]
        public void Condition_NoCheckerAnswersIt_Fails(bool negate)
        {
            var condition = Cond(ConditionType.kHasLevel, negate, "10");

            Assert.That(condition.Check(Checkers(StubChecker.Always(null), StubChecker.Always(null))), Is.False);
        }

        /*------------------------------------------------------------------------------------------------------------------------------
        | --- Condition_PassesTypeAndParametersToCheckers: Checkers receive the condition's type and its parameter array unchanged --- |
        ------------------------------------------------------------------------------------------------------------------------------*/
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

        /*-------------------------------------------------------------------------------------------------------------------------------------------------------
        | --- Condition_SkipsCheckersThatDontHandleIt: Checkers that don't handle the condition (null) are skipped; the one checker that answers decides it --- |
        -------------------------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Checkers that don't handle the condition (null) are skipped; the one checker that answers decides it.")]
        public void Condition_SkipsCheckersThatDontHandleIt()
        {
            var condition = Cond(ConditionType.kHasQuest);

            Assert.That(condition.Check(Checkers(StubChecker.Always(null), StubChecker.Always(true))), Is.True);
            Assert.That(condition.Check(Checkers(StubChecker.Always(null), StubChecker.Always(false))), Is.False);
        }


        /*---------------------
        | --- Disjunction --- |
        ---------------------*/
        /*---------------------------------------------------------------------------------------------------------------------------
        | --- Disjunction_IsTrue_WhenAnyConditionIsTrue: A disjunction (OR) is true when at least one of its conditions is true --- |
        ---------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A disjunction (OR) is true when at least one of its conditions is true.")]
        public void Disjunction_IsTrue_WhenAnyConditionIsTrue()
        {
            var disjunction = Or(CondJson(ConditionType.kHasItem, false), CondJson(ConditionType.kHasQuest, false));

            Assert.That(disjunction.Check(Checkers(TypeKeyed())), Is.True);
        }

        /*---------------------------------------------------------------------------------------------------------------
        | --- Disjunction_IsFalse_WhenNoConditionIsTrue: A disjunction is false when none of its conditions is true --- |
        ---------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A disjunction is false when none of its conditions is true.")]
        public void Disjunction_IsFalse_WhenNoConditionIsTrue()
        {
            var disjunction = Or(CondJson(ConditionType.kHasItem, false), CondJson(ConditionType.kHasItem, false, "x"));

            Assert.That(disjunction.Check(Checkers(TypeKeyed())), Is.False);
        }

        /*------------------------------------------------------------------------------
        | --- Disjunction_Empty_IsFalse: A disjunction with no conditions is false --- |
        ------------------------------------------------------------------------------*/
        [Test]
        [Description("A disjunction with no conditions is false.")]
        public void Disjunction_Empty_IsFalse()
        {
            Assert.That(Or().Check(Checkers(TypeKeyed())), Is.False);
        }

        /*------------------------------------------------------------------------------------------------------------------------------
        | --- Disjunction_StopsAtFirstTrueCondition: A disjunction short-circuits: it stops evaluating at the first true condition --- |
        ------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A disjunction short-circuits: it stops evaluating at the first true condition.")]
        public void Disjunction_StopsAtFirstTrueCondition()
        {
            var checker = TypeKeyed();
            var disjunction = Or(CondJson(ConditionType.kHasQuest, false), CondJson(ConditionType.kHasItem, false));

            disjunction.Check(Checkers(checker));

            Assert.That(checker.Calls, Is.EqualTo(1));
        }


        /*---------------------
        | --- Conjunction --- |
        ---------------------*/
        /*-------------------------------------------------------------------------------------------------------------------------------
        | --- Conjunction_IsTrue_WhenEveryDisjunctionIsTrue: A conjunction (AND) is true when every one of its disjunctions is true --- |
        -------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A conjunction (AND) is true when every one of its disjunctions is true.")]
        public void Conjunction_IsTrue_WhenEveryDisjunctionIsTrue()
        {
            var conjunction = And(
                OrJson(CondJson(ConditionType.kHasQuest, false)),
                OrJson(CondJson(ConditionType.kHasItem, false), CondJson(ConditionType.kHasQuest, false)));

            Assert.That(conjunction.Check(Checkers(TypeKeyed())), Is.True);
        }

        /*-------------------------------------------------------------------------------------------------------------------------
        | --- Conjunction_IsFalse_WhenAnyDisjunctionIsFalse: A conjunction is false when any one of its disjunctions is false --- |
        -------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A conjunction is false when any one of its disjunctions is false.")]
        public void Conjunction_IsFalse_WhenAnyDisjunctionIsFalse()
        {
            var conjunction = And(
                OrJson(CondJson(ConditionType.kHasQuest, false)),
                OrJson(CondJson(ConditionType.kHasItem, false)));

            Assert.That(conjunction.Check(Checkers(TypeKeyed())), Is.False);
        }

        /*------------------------------------------------------------------------------------------------------------------------------------
        | --- Conjunction_Negation_FlipsOneCondition: Negating a failing condition inside a conjunction makes the whole conjunction pass --- |
        ------------------------------------------------------------------------------------------------------------------------------------*/
        [Test]
        [Description("Negating a failing condition inside a conjunction makes the whole conjunction pass.")]
        public void Conjunction_Negation_FlipsOneCondition()
        {
            var conjunction = And(
                OrJson(CondJson(ConditionType.kHasQuest, false)),
                OrJson(CondJson(ConditionType.kHasItem, true)));

            Assert.That(conjunction.Check(Checkers(TypeKeyed())), Is.True);
        }

        /*------------------------------------------------------------------------------------------------------------------------------------
        | --- Conjunction_StopsAtFirstFalseDisjunction: A conjunction short-circuits: it stops evaluating at the first false disjunction --- |
        ------------------------------------------------------------------------------------------------------------------------------------*/
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

        /*-------------------------------------------------------------------------------------------
        | --- Conjunction_WithNoDisjunctions_IsTrue: A conjunction with no disjunctions is true --- |
        -------------------------------------------------------------------------------------------*/
        [Test]
        [Description("A conjunction with no disjunctions is true.")]
        public void Conjunction_WithNoDisjunctions_IsTrue()
        {
            Assert.That(And().Check(Checkers(TypeKeyed())), Is.True);
        }
    }
}