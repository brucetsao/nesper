///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

using com.espertech.esper.core.context.util;
using com.espertech.esper.core.support;
using com.espertech.esper.view.std;

using NUnit.Framework;

namespace com.espertech.esper.view.window
{
    [TestFixture]
    public class TestLengthWindowViewFactory 
    {
        private LengthWindowViewFactory _factory;
    
        [SetUp]
        public void SetUp()
        {
            _factory = new LengthWindowViewFactory();
        }
    
        [Test]
        public void TestSetParameters()
        {
            TryParameter(new Object[] {10}, 10);
    
            TryInvalidParameter("TheString");
            TryInvalidParameter(true);
            TryInvalidParameter(1.1d);
            TryInvalidParameter(0);
        }
    
        [Test]
        public void TestCanReuse()
        {
            AgentInstanceContext agentInstanceContext = SupportStatementContextFactory.MakeAgentInstanceContext();
            _factory.SetViewParameters(SupportStatementContextFactory.MakeViewContext(), TestViewSupport.ToExprListBean(new Object[] { 1000 }));
            Assert.IsFalse(_factory.CanReuse(new FirstElementView(null), agentInstanceContext));
            Assert.IsFalse(_factory.CanReuse(new LengthWindowView(SupportStatementContextFactory.MakeAgentInstanceViewFactoryContext(), _factory, 1, null), agentInstanceContext));
            Assert.IsTrue(_factory.CanReuse(new LengthWindowView(SupportStatementContextFactory.MakeAgentInstanceViewFactoryContext(), _factory, 1000, null), agentInstanceContext));
        }
    
        private void TryInvalidParameter(Object param)
        {
            try
            {
    
                _factory.SetViewParameters(SupportStatementContextFactory.MakeViewContext(), TestViewSupport.ToExprListBean(new Object[] {param}));
                Assert.Fail();
            }
            catch (ViewParameterException ex)
            {
                // expected
            }
        }
    
        private void TryParameter(Object[] param, int size)
        {
            var factory = new LengthWindowViewFactory();
            factory.SetViewParameters(SupportStatementContextFactory.MakeViewContext(), TestViewSupport.ToExprListBean(param));
            var view = (LengthWindowView) factory.MakeView(SupportStatementContextFactory.MakeAgentInstanceViewFactoryContext());
            Assert.AreEqual(size, view.Size);
        }
    }
}
