///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2017 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;

using com.espertech.esper.client;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.compat;
using com.espertech.esper.compat.collections;
using com.espertech.esper.compat.logging;
using com.espertech.esper.supportregression.events;
using com.espertech.esper.supportregression.execution;
using com.espertech.esper.util;

// using static org.junit.Assert.assertEquals;
// using static org.junit.Assert.assertNotNull;

using NUnit.Framework;

namespace com.espertech.esper.regression.events.xml
{
    public class ExecEventXMLSchemaWithRestriction : RegressionExecution {
        public static readonly string CLASSLOADER_SCHEMA_WITH_RESTRICTION_URI = "regression/simpleSchemaWithRestriction.xsd";
    
        public override void Configure(Configuration configuration) {
            var eventTypeMeta = new ConfigurationEventTypeXMLDOM();
            eventTypeMeta.RootElementName = "order";
            InputStream schemaStream = typeof(ExecEventXMLSchemaWithRestriction).ClassLoader.GetResourceAsStream(CLASSLOADER_SCHEMA_WITH_RESTRICTION_URI);
            Assert.IsNotNull(schemaStream);
            string schemaText = FileUtil.LinesToText(FileUtil.ReadFile(schemaStream));
            eventTypeMeta.SchemaText = schemaText;
            configuration.AddEventType("OrderEvent", eventTypeMeta);
        }
    
        public override void Run(EPServiceProvider epService) {
            var updateListener = new SupportUpdateListener();
    
            string text = "select order_amount from OrderEvent";
            EPStatement stmt = epService.EPAdministrator.CreateEPL(text);
            stmt.AddListener(updateListener);
    
            SupportXML.SendEvent(epService.EPRuntime,
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                            "<order>\n" +
                            "<order_amount>202.1</order_amount>" +
                            "</order>");
            EventBean theEvent = updateListener.LastNewData[0];
            Assert.AreEqual(typeof(double?), theEvent.Get("order_amount").Class);
            Assert.AreEqual(202.1d, theEvent.Get("order_amount"));
            updateListener.Reset();
        }
    }
} // end of namespace