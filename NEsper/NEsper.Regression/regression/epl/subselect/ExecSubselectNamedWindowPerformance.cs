///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

using com.espertech.esper.client;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;
using com.espertech.esper.compat.logging;
using com.espertech.esper.supportregression.bean;
using com.espertech.esper.supportregression.execution;
using com.espertech.esper.util;

// using static org.junit.Assert.assertTrue;

using NUnit.Framework;

namespace com.espertech.esper.regression.epl.subselect
{
    public class ExecSubselectNamedWindowPerformance : RegressionExecution {
        public override void Configure(Configuration configuration) {
            configuration.EngineDefaults.Logging.IsEnableQueryPlan = true;
        }
    
        public override void Run(EPServiceProvider epService) {
            RunAssertionConstantValue(epService);
            RunAssertionKeyAndRange(epService);
            RunAssertionRange(epService);
            RunAssertionKeyedRange(epService);
            RunAssertionNoShare(epService);
            RunAssertionShare(epService);
            RunAssertionShareCreate(epService);
            RunAssertionDisableShare(epService);
            RunAssertionDisableShareCreate(epService);
        }
    
        private void RunAssertionConstantValue(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType("SupportBeanRange", typeof(SupportBeanRange));
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
    
            RunConstantValueAssertion(epService, false, false);
            RunConstantValueAssertion(epService, true, false);
            RunConstantValueAssertion(epService, true, true);
        }
    
        private void RunConstantValueAssertion(EPServiceProvider epService, bool indexShare, bool buildIndex) {
            string createEpl = "create window MyWindow#keepall as select * from SupportBean";
            if (indexShare) {
                createEpl = "@Hint('enable_window_subquery_indexshare') " + createEpl;
            }
            epService.EPAdministrator.CreateEPL(createEpl);
    
            if (buildIndex) {
                epService.EPAdministrator.CreateEPL("create index idx1 on MyWindow(theString hash, intPrimitive btree)");
            }
            epService.EPAdministrator.CreateEPL("insert into MyWindow select * from SupportBean");
    
            // preload
            for (int i = 0; i < 10000; i++) {
                var bean = new SupportBean("E" + i, i);
                bean.DoublePrimitive = i;
                epService.EPRuntime.SendEvent(bean);
            }
    
            // single-field compare
            string[] fields = "val".Split(',');
            string eplSingle = "select (select intPrimitive from MyWindow where theString = 'E9734') as val from SupportBeanRange sbr";
            EPStatement stmtSingle = epService.EPAdministrator.CreateEPL(eplSingle);
            var listener = new SupportUpdateListener();
            stmtSingle.AddListener(listener);
    
            long startTime = DateTimeHelper.CurrentTimeMillis;
            for (int i = 0; i < 1000; i++) {
                epService.EPRuntime.SendEvent(new SupportBeanRange("R", "", -1, -1));
                EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{9734});
            }
            long delta = DateTimeHelper.CurrentTimeMillis - startTime;
            Assert.IsTrue("delta=" + delta, delta < 500);
            stmtSingle.Dispose();
    
            // two-field compare
            string eplTwoHash = "select (select intPrimitive from MyWindow where theString = 'E9736' and intPrimitive = 9736) as val from SupportBeanRange sbr";
            EPStatement stmtTwoHash = epService.EPAdministrator.CreateEPL(eplTwoHash);
            stmtTwoHash.AddListener(listener);
    
            startTime = DateTimeHelper.CurrentTimeMillis;
            for (int i = 0; i < 1000; i++) {
                epService.EPRuntime.SendEvent(new SupportBeanRange("R", "", -1, -1));
                EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{9736});
            }
            delta = DateTimeHelper.CurrentTimeMillis - startTime;
            Assert.IsTrue("delta=" + delta, delta < 500);
            stmtTwoHash.Dispose();
    
            // range compare single
            if (buildIndex) {
                epService.EPAdministrator.CreateEPL("create index idx2 on MyWindow(intPrimitive btree)");
            }
            string eplSingleBTree = "select (select intPrimitive from MyWindow where intPrimitive between 9735 and 9735) as val from SupportBeanRange sbr";
            EPStatement stmtSingleBtree = epService.EPAdministrator.CreateEPL(eplSingleBTree);
            stmtSingleBtree.AddListener(listener);
    
            startTime = DateTimeHelper.CurrentTimeMillis;
            for (int i = 0; i < 1000; i++) {
                epService.EPRuntime.SendEvent(new SupportBeanRange("R", "", -1, -1));
                EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{9735});
            }
            delta = DateTimeHelper.CurrentTimeMillis - startTime;
            Assert.IsTrue("delta=" + delta, delta < 500);
            stmtSingleBtree.Dispose();
    
            // range compare composite
            string eplComposite = "select (select intPrimitive from MyWindow where theString = 'E9738' and intPrimitive between 9738 and 9738) as val from SupportBeanRange sbr";
            EPStatement stmtComposite = epService.EPAdministrator.CreateEPL(eplComposite);
            stmtComposite.AddListener(listener);
    
            startTime = DateTimeHelper.CurrentTimeMillis;
            for (int i = 0; i < 1000; i++) {
                epService.EPRuntime.SendEvent(new SupportBeanRange("R", "", -1, -1));
                EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{9738});
            }
            delta = DateTimeHelper.CurrentTimeMillis - startTime;
            Assert.IsTrue("delta=" + delta, delta < 500);
            stmtComposite.Dispose();
    
            // destroy all
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionKeyAndRange(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType("SupportBeanRange", typeof(SupportBeanRange));
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
    
            RunKeyAndRangeAssertion(epService, false, false);
            RunKeyAndRangeAssertion(epService, true, false);
            RunKeyAndRangeAssertion(epService, true, true);
        }
    
        private void RunKeyAndRangeAssertion(EPServiceProvider epService, bool indexShare, bool buildIndex) {
            string createEpl = "create window MyWindow#keepall as select * from SupportBean";
            if (indexShare) {
                createEpl = "@Hint('enable_window_subquery_indexshare') " + createEpl;
            }
            epService.EPAdministrator.CreateEPL(createEpl);
    
            if (buildIndex) {
                epService.EPAdministrator.CreateEPL("create index idx1 on MyWindow(theString hash, intPrimitive btree)");
            }
            epService.EPAdministrator.CreateEPL("insert into MyWindow select * from SupportBean");
    
            // preload
            for (int i = 0; i < 10000; i++) {
                string theString = i < 5000 ? "A" : "B";
                epService.EPRuntime.SendEvent(new SupportBean(theString, i));
            }
    
            string[] fields = "cols.mini,cols.maxi".Split(',');
            string queryEpl = "select (select min(intPrimitive) as mini, max(intPrimitive) as maxi from MyWindow where theString = sbr.key and intPrimitive between sbr.rangeStart and sbr.rangeEnd) as cols from SupportBeanRange sbr";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(queryEpl);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            long startTime = DateTimeHelper.CurrentTimeMillis;
            for (int i = 0; i < 1000; i++) {
                epService.EPRuntime.SendEvent(new SupportBeanRange("R1", "A", 300, 312));
                EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{300, 312});
            }
            long delta = DateTimeHelper.CurrentTimeMillis - startTime;
            Assert.IsTrue("delta=" + delta, delta < 500);
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionRange(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType("SupportBeanRange", typeof(SupportBeanRange));
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
    
            RunRangeAssertion(epService, false, false);
            RunRangeAssertion(epService, true, false);
            RunRangeAssertion(epService, true, true);
        }
    
        private void RunRangeAssertion(EPServiceProvider epService, bool indexShare, bool buildIndex) {
            string createEpl = "create window MyWindow#keepall as select * from SupportBean";
            if (indexShare) {
                createEpl = "@Hint('enable_window_subquery_indexshare') " + createEpl;
            }
            epService.EPAdministrator.CreateEPL(createEpl);
    
            if (buildIndex) {
                epService.EPAdministrator.CreateEPL("create index idx1 on MyWindow(intPrimitive btree)");
            }
            epService.EPAdministrator.CreateEPL("insert into MyWindow select * from SupportBean");
    
            // preload
            for (int i = 0; i < 10000; i++) {
                epService.EPRuntime.SendEvent(new SupportBean("E1", i));
            }
    
            string[] fields = "cols.mini,cols.maxi".Split(',');
            string queryEpl = "select (select min(intPrimitive) as mini, max(intPrimitive) as maxi from MyWindow where intPrimitive between sbr.rangeStart and sbr.rangeEnd) as cols from SupportBeanRange sbr";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(queryEpl);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            long startTime = DateTimeHelper.CurrentTimeMillis;
            for (int i = 0; i < 1000; i++) {
                epService.EPRuntime.SendEvent(new SupportBeanRange("R1", "K", 300, 312));
                EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{300, 312});
            }
            long delta = DateTimeHelper.CurrentTimeMillis - startTime;
            Assert.IsTrue("delta=" + delta, delta < 500);
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionKeyedRange(EPServiceProvider epService) {
            epService.EPAdministrator.Configuration.AddEventType("SupportBeanRange", typeof(SupportBeanRange));
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
    
            string createEpl = "create window MyWindow#keepall as select * from SupportBean";
            epService.EPAdministrator.CreateEPL(createEpl);
            epService.EPAdministrator.CreateEPL("insert into MyWindow select * from SupportBean");
    
            // preload
            for (int i = 0; i < 10000; i++) {
                string key = i < 5000 ? "A" : "B";
                epService.EPRuntime.SendEvent(new SupportBean(key, i));
            }
    
            string[] fields = "cols.mini,cols.maxi".Split(',');
            string queryEpl = "select (select min(intPrimitive) as mini, max(intPrimitive) as maxi from MyWindow " +
                    "where theString = sbr.key and intPrimitive between sbr.rangeStart and sbr.rangeEnd) as cols from SupportBeanRange sbr";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(queryEpl);
            var listener = new SupportUpdateListener();
            stmt.AddListener(listener);
    
            long startTime = DateTimeHelper.CurrentTimeMillis;
            for (int i = 0; i < 500; i++) {
                epService.EPRuntime.SendEvent(new SupportBeanRange("R1", "A", 299, 313));
                EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{299, 313});
    
                epService.EPRuntime.SendEvent(new SupportBeanRange("R2", "B", 7500, 7510));
                EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{7500, 7510});
            }
            long delta = DateTimeHelper.CurrentTimeMillis - startTime;
            Assert.IsTrue("delta=" + delta, delta < 500);
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void RunAssertionNoShare(EPServiceProvider epService) {
            TryAssertion(epService, false, false, false);
        }
    
        private void RunAssertionShare(EPServiceProvider epService) {
            TryAssertion(epService, true, false, false);
        }
    
        private void RunAssertionShareCreate(EPServiceProvider epService) {
            TryAssertion(epService, true, false, true);
        }
    
        private void RunAssertionDisableShare(EPServiceProvider epService) {
            TryAssertion(epService, true, true, false);
        }
    
        private void RunAssertionDisableShareCreate(EPServiceProvider epService) {
            TryAssertion(epService, true, true, true);
        }
    
        private void TryAssertion(EPServiceProvider epService, bool enableIndexShareCreate, bool disableIndexShareConsumer, bool createExplicitIndex) {
            epService.EPAdministrator.CreateEPL("create schema EventSchema(e0 string, e1 int, e2 string)");
    
            string createEpl = "create window MyWindow#keepall as select * from SupportBean";
            if (enableIndexShareCreate) {
                createEpl = "@Hint('enable_window_subquery_indexshare') " + createEpl;
            }
            epService.EPAdministrator.CreateEPL(createEpl);
            epService.EPAdministrator.CreateEPL("insert into MyWindow select * from SupportBean");
    
            if (createExplicitIndex) {
                epService.EPAdministrator.CreateEPL("create index MyIndex on MyWindow (theString)");
            }
    
            string consumeEpl = "select e0, (select theString from MyWindow where intPrimitive = es.e1 and theString = es.e2) as val from EventSchema as es";
            if (disableIndexShareConsumer) {
                consumeEpl = "@Hint('disable_window_subquery_indexshare') " + consumeEpl;
            }
            EPStatement consumeStmt = epService.EPAdministrator.CreateEPL(consumeEpl);
            var listener = new SupportUpdateListener();
            consumeStmt.AddListener(listener);
    
            string[] fields = "e0,val".Split(',');
    
            // test once
            epService.EPRuntime.SendEvent(new SupportBean("WX", 10));
            SendEvent(epService, "E1", 10, "WX");
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{"E1", "WX"});
    
            // preload
            for (int i = 0; i < 10000; i++) {
                epService.EPRuntime.SendEvent(new SupportBean("W" + i, i));
            }
    
            long startTime = DateTimeHelper.CurrentTimeMillis;
            for (int i = 0; i < 5000; i++) {
                SendEvent(epService, "E" + i, i, "W" + i);
                EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[]{"E" + i, "W" + i});
            }
            long endTime = DateTimeHelper.CurrentTimeMillis;
            long delta = endTime - startTime;
            Assert.IsTrue("delta=" + delta, delta < 500);
    
            epService.EPAdministrator.DestroyAllStatements();
        }
    
        private void SendEvent(EPServiceProvider epService, string e0, int e1, string e2) {
            var theEvent = new LinkedHashMap<string, object>();
            theEvent.Put("e0", e0);
            theEvent.Put("e1", e1);
            theEvent.Put("e2", e2);
            if (EventRepresentationChoice.GetEngineDefault(epService).IsObjectArrayEvent()) {
                epService.EPRuntime.SendEvent(theEvent.Values().ToArray(), "EventSchema");
            } else {
                epService.EPRuntime.SendEvent(theEvent, "EventSchema");
            }
        }
    }
} // end of namespace