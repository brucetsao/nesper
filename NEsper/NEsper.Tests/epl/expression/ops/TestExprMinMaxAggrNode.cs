///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

using com.espertech.esper.epl.expression.core;
using com.espertech.esper.epl.expression.methodagg;
using com.espertech.esper.epl.expression.ops;
using com.espertech.esper.supportunit.epl;
using com.espertech.esper.type;
using com.espertech.esper.util.support;

using NUnit.Framework;

namespace com.espertech.esper.epl.expression
{
    public class TestExprMinMaxAggrNode : TestExprAggregateNodeAdapter
    {
        private ExprMinMaxAggrNode _maxNode;
        private ExprMinMaxAggrNode _minNode;
    
        [SetUp]
        public void SetUp()
        {
            _maxNode = new ExprMinMaxAggrNode(false, MinMaxTypeEnum.MAX, false, false);
            _minNode = new ExprMinMaxAggrNode(false, MinMaxTypeEnum.MIN, false, false);
    
            ValidatedNodeToTest= MakeNode(MinMaxTypeEnum.MAX, 5, typeof(int));
        }
    
        [Test]
        public void TestGetType()
        {
            _maxNode.AddChildNode(new SupportExprNode(typeof(int)));
            SupportExprNodeFactory.Validate3Stream(_maxNode);
            Assert.AreEqual(typeof(int?), _maxNode.ReturnType);

            _minNode.AddChildNode(new SupportExprNode(typeof(float?)));
            SupportExprNodeFactory.Validate3Stream(_minNode);
            Assert.AreEqual(typeof(float?), _minNode.ReturnType);

            _maxNode = new ExprMinMaxAggrNode(false, MinMaxTypeEnum.MAX, false, false);
            _maxNode.AddChildNode(new SupportExprNode(typeof(short)));
            SupportExprNodeFactory.Validate3Stream(_maxNode);
            Assert.AreEqual(typeof(short?), _maxNode.ReturnType);
        }
    
        [Test]
        public void TestToExpressionString()
        {
            // Build sum(4-2)
            ExprMathNode arithNodeChild = new ExprMathNode(MathArithTypeEnum.SUBTRACT, false, false);
            arithNodeChild.AddChildNode(new SupportExprNode(4));
            arithNodeChild.AddChildNode(new SupportExprNode(2));
    
            _maxNode.AddChildNode(arithNodeChild);
            Assert.AreEqual("max(4-2)", _maxNode.ToExpressionStringMinPrecedenceSafe());
            _minNode.AddChildNode(arithNodeChild);
            Assert.AreEqual("min(4-2)", _minNode.ToExpressionStringMinPrecedenceSafe());
        }
    
        [Test]
        public void TestValidate()
        {
            // Must have exactly 1 subnodes
            try
            {
                _minNode.Validate(SupportExprValidationContextFactory.MakeEmpty());
                Assert.Fail();
            }
            catch (ExprValidationException ex)
            {
                // Expected
            }
    
            // Must have only number-type subnodes
            _minNode = new ExprMinMaxAggrNode(false, MinMaxTypeEnum.MIN, true, false);
            _minNode.AddChildNode(new SupportExprNode(typeof(string)));
            _minNode.AddChildNode(new SupportExprNode(typeof(int)));
            try
            {
                _minNode.Validate(SupportExprValidationContextFactory.Make(new SupportStreamTypeSvc3Stream()));
                Assert.Fail();
            }
            catch (ExprValidationException ex)
            {
                // Expected
            }
        }
    
        [Test]
        public void TestEqualsNode()
        {
            Assert.IsTrue(_minNode.EqualsNode(_minNode));
            Assert.IsFalse(_maxNode.EqualsNode(_minNode));
            Assert.IsFalse(_minNode.EqualsNode(new ExprSumNode(false)));
        }

        private ExprMinMaxAggrNode MakeNode(MinMaxTypeEnum minMaxType, Object value, Type type)
        {
            ExprMinMaxAggrNode minMaxNode = new ExprMinMaxAggrNode(false, minMaxType, false, false);
            minMaxNode.AddChildNode(new SupportExprNode(value, type));
            SupportExprNodeFactory.Validate3Stream(minMaxNode);
            return minMaxNode;
        }
    }
}
