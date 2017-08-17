using System;
//using System.Drawing;
//using System.Collections;
//using System.ComponentModel;

//using SmartQuant.FIX;
//using SmartQuant.Data;
using SmartQuant.Instruments;
using SmartQuant.Trading;

[StrategyComponent("{0c19f913-f3e5-4c53-9908-4b87403f0616}", ComponentType.MarketManager , Name="mktSingle", Description="")] 
public class mktSingle : MarketManager
{
	//string sSymbol = "LFC";
  string sSymbol = "GOOG";
	
	//[Category("Instrument"), Description("Instrument")]
	//public string SymbolName {
//		get { return sSymbol; }
//		set { sSymbol = value; }
//	}
	
	public override void Init()
	{
		
		Instrument instrument;
		
		instrument = InstrumentManager.Instruments[ sSymbol ];
		AddInstrument(instrument);
		//Console.WriteLine( "Market added {0}", sSymbol );
	}
}