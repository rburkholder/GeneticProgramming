using System;
using System.Drawing;
using System.ComponentModel;

//using SmartQuant;
using SmartQuant.FIX;
//using SmartQuant.Data;
//using SmartQuant.FIXData;
using SmartQuant.Trading;
//using SmartQuant.Series;
//using SmartQuant.Optimization;
//using SmartQuant.Indicators;
//using SmartQuant.Instruments;
//using SmartQuant.Execution;
//using SmartQuant.Providers;

//namespace UI {

  public class aslippageprovider : SmartQuant.Simulation.ISlippageProvider {
    #region ISlippageProvider Members

    public double GetExecutionPrice( ExecutionReport report ) {
      //report.Price
      report.Side = Side.Buy;
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion
  }

  [StrategyComponent("{52728743-527f-4e40-a128-dd37387e6304}", ComponentType.SimulationManager, Name = "simQuotesAndTicks", Description = "")]
  public class simQuotesAndTicks : SimulationManager {
    public override void Init() {
      CommissionProvider.Commission = 0.01;

      //SendMarketDataRequest("Bar.Time.60");
      SendMarketDataRequest("Quote");
      SendMarketDataRequest("Trade");
      //SendMarketDataRequest("Daily");
      base.FillOnBar = false;
      base.FillOnQuote = true;
      base.FillOnTrade = false;
      base.FillOnQuoteMode = SmartQuant.Simulation.FillOnQuoteMode.NextQuote;
      //EntryDate = new DateTime(2006, 02, 27, 10, 30, 00);
      //ExitDate = new DateTime(2006, 02, 27, 10, 36, 00);
      EntryDate = new DateTime(2007, 01, 08, 11, 45, 00);
      //EntryDate = new DateTime(2006, 03, 23, 9, 30, 00);
      //EntryDate = new DateTime(2006, 03, 23, 11, 42, 00);
      ExitDate  = new DateTime(2007, 01, 10, 17, 00, 00);
      Cash = 100000.00;
      
    }
    
    
  }
//}



