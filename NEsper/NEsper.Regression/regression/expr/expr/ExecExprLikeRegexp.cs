///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

using com.espertech.esper.client;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.client.soda;
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.execution;
using com.espertech.esper.util;

// using static junit.framework.TestCase.*;
// using static org.junit.Assert.assertEquals;

using NUnit.Framework;

namespace com.espertech.esper.regression.expr.expr
{
    public class ExecExprLikeRegexp : RegressionExecution {
        public override void Run(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
    
            RunAssertionRegexpFilterWithDanglingMetaCharacter(epService);
            RunAssertionLikeRegexStringAndNull(epService);
            RunAssertionLikeRegexEscapedChar(epService);
            RunAssertionLikeRegexStringAndNull_OM(epService);
            RunAssertionLikeRegexStringAndNull_Compile(epService);
            RunAssertionInvalidLikeRegEx(epService);
            RunAssertionLikeRegexNumericAndNull(epService);
        }
    
        private void RunAssertionRegexpFilterWithDanglingMetaCharacter(EPServiceProvider epService) {
            EPStatement stmt = epService.EPAdministrator.CreateEPL("select * from SupportBean where theString regexp \"*any*\"");
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SupportBean());
            Assert.IsFalse(listener.IsInvoked);
    
            stmt.Dispose();
        }
    
        private void RunAssertionLikeRegexStringAndNull(EPServiceProvider epService) {
            string caseExpr = "select p00 like p01 as r1, " +
                    " p00 like p01 escape \"!\" as r2," +
                    " p02 regexp p03 as r3 " +
                    " from " + typeof(SupportBean_S0).Name;
    
            EPStatement stmt = epService.EPAdministrator.CreateEPL(caseExpr);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            RunLikeRegexStringAndNull(epService, listener);
    
            stmt.Dispose();
        }
    
        private void RunAssertionLikeRegexEscapedChar(EPServiceProvider epService) {
            string caseExpr = "select p00 regexp '\\\\w*-ABC' as result from " + typeof(SupportBean_S0).Name;
    
            EPStatement stmt = epService.EPAdministrator.CreateEPL(caseExpr);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(-1, "TBT-ABC"));
            Assert.IsTrue((bool?) listener.AssertOneGetNewAndReset().Get("result"));
    
            epService.EPRuntime.SendEvent(new SupportBean_S0(-1, "TBT-BC"));
            Assert.IsFalse((bool?) listener.AssertOneGetNewAndReset().Get("result"));
    
            stmt.Dispose();
        }
    
        private void RunAssertionLikeRegexStringAndNull_OM(EPServiceProvider epService) {
            string stmtText = "select p00 like p01 as r1, " +
                    "p00 like p01 escape \"!\" as r2, " +
                    "p02 regexp p03 as r3 " +
                    "from " + typeof(SupportBean_S0).Name;
    
            var model = new EPStatementObjectModel();
            model.SelectClause = SelectClause.Create()
                .Add(Expressions.Like(Expressions.Property("p00"), Expressions.Property("p01")), "r1")
                .Add(Expressions.Like(Expressions.Property("p00"), Expressions.Property("p01"), Expressions.Constant("!")), "r2")
                .Add(Expressions.Regexp(Expressions.Property("p02"), Expressions.Property("p03")), "r3");

            model.FromClause = FromClause.Create(FilterStream.Create(typeof(SupportBean_S0).FullName));
            model = (EPStatementObjectModel) SerializableObjectCopier.Copy(model);
            Assert.AreEqual(stmtText, model.ToEPL());
    
            EPStatement stmt = epService.EPAdministrator.Create(model);
            var testListener = new SupportUpdateListener();
            stmt.AddListener(testListener);
    
            RunLikeRegexStringAndNull(epService, testListener);
    
            stmt.Dispose();
    
            string epl = "select * from " + typeof(SupportBean).FullName + "(theString not like \"foo%\")";
            EPPreparedStatement eps = epService.EPAdministrator.PrepareEPL(epl);
            EPStatement statement = epService.EPAdministrator.Create(eps);
            Assert.AreEqual(epl, statement.Text);
            statement.Dispose();
    
            epl = "select * from " + typeof(SupportBean).FullName + "(theString not regexp \"foo\")";
            eps = epService.EPAdministrator.PrepareEPL(epl);
            statement = epService.EPAdministrator.Create(eps);
            Assert.AreEqual(epl, statement.Text);
            statement.Dispose();
        }
    
        private void RunAssertionLikeRegexStringAndNull_Compile(EPServiceProvider epService) {
            string stmtText = "select p00 like p01 as r1, " +
                    "p00 like p01 escape \"!\" as r2, " +
                    "p02 regexp p03 as r3 " +
                    "from " + typeof(SupportBean_S0).Name;
    
            EPStatementObjectModel model = epService.EPAdministrator.CompileEPL(stmtText);
            model = (EPStatementObjectModel) SerializableObjectCopier.Copy(model);
            Assert.AreEqual(stmtText, model.ToEPL());
    
            EPStatement stmt = epService.EPAdministrator.Create(model);
            var testListener = new SupportUpdateListener();
            stmt.AddListener(testListener);
    
            RunLikeRegexStringAndNull(epService, testListener);
    
            stmt.Dispose();
        }
    
        private void RunLikeRegexStringAndNull(EPServiceProvider epService, SupportUpdateListener listener) {
            SendS0Event(epService, "a", "b", "c", "d");
            AssertReceived(listener, new Object[][]{new object[] {"r1", false}, new object[] {"r2", false}, new object[] {"r3", false}});
    
            SendS0Event(epService, null, "b", null, "d");
            AssertReceived(listener, new Object[][]{new object[] {"r1", null}, new object[] {"r2", null}, new object[] {"r3", null}});
    
            SendS0Event(epService, "a", null, "c", null);
            AssertReceived(listener, new Object[][]{new object[] {"r1", null}, new object[] {"r2", null}, new object[] {"r3", null}});
    
            SendS0Event(epService, null, null, null, null);
            AssertReceived(listener, new Object[][]{new object[] {"r1", null}, new object[] {"r2", null}, new object[] {"r3", null}});
    
            SendS0Event(epService, "abcdef", "%de_", "a", "[a-c]");
            AssertReceived(listener, new Object[][]{new object[] {"r1", true}, new object[] {"r2", true}, new object[] {"r3", true}});
    
            SendS0Event(epService, "abcdef", "b%de_", "d", "[a-c]");
            AssertReceived(listener, new Object[][]{new object[] {"r1", false}, new object[] {"r2", false}, new object[] {"r3", false}});
    
            SendS0Event(epService, "!adex", "!%de_", "", ".");
            AssertReceived(listener, new Object[][]{new object[] {"r1", true}, new object[] {"r2", false}, new object[] {"r3", false}});
    
            SendS0Event(epService, "%dex", "!%de_", "a", ".");
            AssertReceived(listener, new Object[][]{new object[] {"r1", false}, new object[] {"r2", true}, new object[] {"r3", true}});
        }
    
        private void RunAssertionInvalidLikeRegEx(EPServiceProvider epService) {
            TryInvalid(epService, "intPrimitive like 'a' escape null");
            TryInvalid(epService, "intPrimitive like boolPrimitive");
            TryInvalid(epService, "boolPrimitive like string");
            TryInvalid(epService, "string like string escape intPrimitive");
    
            TryInvalid(epService, "intPrimitive regexp doublePrimitve");
            TryInvalid(epService, "intPrimitive regexp boolPrimitive");
            TryInvalid(epService, "boolPrimitive regexp string");
            TryInvalid(epService, "string regexp intPrimitive");
        }
    
        private void RunAssertionLikeRegexNumericAndNull(EPServiceProvider epService) {
            string caseExpr = "select intBoxed like '%01%' as r1, " +
                    " doubleBoxed regexp '[0-9][0-9].[0-9]' as r2 " +
                    " from " + typeof(SupportBean).FullName;
    
            EPStatement selectTestCase = epService.EPAdministrator.CreateEPL(caseExpr);
            var testListener = new SupportUpdateListener();
            selectTestCase.AddListener(testListener);
    
            SendSupportBeanEvent(epService, 101, 1.1);
            AssertReceived(testListener, new Object[][]{new object[] {"r1", true}, new object[] {"r2", false}});
    
            SendSupportBeanEvent(epService, 102, 11d);
            AssertReceived(testListener, new Object[][]{new object[] {"r1", false}, new object[] {"r2", true}});
    
            SendSupportBeanEvent(epService, null, null);
            AssertReceived(testListener, new Object[][]{new object[] {"r1", null}, new object[] {"r2", null}});
        }
    
        private void TryInvalid(EPServiceProvider epService, string expr) {
            try {
                string statement = "select " + expr + " from " + typeof(SupportBean).FullName;
                epService.EPAdministrator.CreateEPL(statement);
                Assert.Fail();
            } catch (EPException ex) {
                // expected
            }
        }
    
        private void AssertReceived(SupportUpdateListener testListener, Object[][] objects) {
            EventBean theEvent = testListener.AssertOneGetNewAndReset();
            foreach (Object[] @object in objects) {
                string key = (string) @object[0];
                Object result = @object[1];
                Assert.AreEqual(result, theEvent.Get(key), "key=" + key + " result=" + result);
            }
        }
    
        private void SendS0Event(EPServiceProvider epService, string p00, string p01, string p02, string p03) {
            var bean = new SupportBean_S0(-1, p00, p01, p02, p03);
            epService.EPRuntime.SendEvent(bean);
        }
    
        private void SendSupportBeanEvent(EPServiceProvider epService, int? intBoxed, double? doubleBoxed) {
            var bean = new SupportBean();
            bean.IntBoxed = intBoxed;
            bean.DoubleBoxed = doubleBoxed;
            epService.EPRuntime.SendEvent(bean);
        }
    }
} // end of namespace