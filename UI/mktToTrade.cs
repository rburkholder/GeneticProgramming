using System;
using System.ComponentModel;
using System.Collections;
using System.Data;
using System.Data.SqlClient;

//using QDCustom;
using OneUnified.SmartQuant;
//using OneUnified.

using SmartQuant.FIX;
using SmartQuant.Data;
using SmartQuant.Series;
using SmartQuant.Indicators;
using SmartQuant.Instruments;
using SmartQuant.Execution;
using SmartQuant.Trading;
using SmartQuant.Providers;
using SmartQuant.Optimization;

[StrategyComponent("{f6e7b160-1d92-4f46-af68-8d451783addc}", ComponentType.MarketManager , Name="mktToTrade", Description="")] 
public class mktToTrade : MarketManager
{
	string sTradeSystem;
	
	//Strategy strat;
	StrategyBase sb;
	//MarketManager mm;
	//MetaStrategy ms;
	
	bool bLossShown = false;
	
	//DateTime dtCurrentDay;
	[Category("Parameter"), Description("Trade System")]
	public string TradeSystem
	{
		get { return sTradeSystem; }
		set { sTradeSystem = value; }		
	}
	
	public mktToTrade (){
		//sTradeSystem = "mkt5DayVolatilityBgn";	
		sTradeSystem = "20070108";	
	}
	
	public override void Init()
	{
		sb = base.StrategyBase;
		//mm = strat.MarketManager;
		//ms = sb.MetaStrategy;
		
		Console.WriteLine( "Mode {0}", sb.MetaStrategyBase.MetaStrategyMode );
		
		TradeDB db = new TradeDB();
      
		db.Open();
      
		SqlCommand cmd;
		SqlDataReader dr;
		//DateTime inc;
		Instrument instrument;
		string sSymbol;
		
		string s = "select symbol from ToTrade"
			+ " where tradesystem = @tradesystem"
			+ " and sixmonpossd <  0.97 * sixmonposmean"
			+ " and sixmonnegsd < -0.97 * sixmonnegmean"
			+ " and daycl > 10"
			+ " and daycl < 125"
			+ " and sixmonposmean >  0.12"
			+ " and sixmonnegmean < -0.12"
			+ " order by symbol";
		cmd = new SqlCommand( s, db.Connection );
		cmd.Parameters.Add("@tradesystem", SqlDbType.VarChar);
		cmd.Parameters["@tradesystem"].Value = sTradeSystem;
		
		int cnt = 0;
		dr = cmd.ExecuteReader();
		while ( dr.Read() ) {
			cnt++;
			sSymbol = dr.GetString(0);
			instrument = InstrumentManager.Instruments[ sSymbol ];
			AddInstrument(instrument);
			//Console.WriteLine( "{0}.MarketManager", sSymbol );
		}
		
		db.Close();
		//Console.WriteLine( "{0} symbols added.", cnt );
		//dtCurrentDay = new DateTime( 0 );
		//bRecordDayStartEquity = false;
		
		//Portfolio.CompositionChanged += new EventHandler(Portfolio_CompositionChanged);
		//Portfolio.ValueChanged += new PositionEventHandler(Portfolio_ValueChanged);
		//Portfolio.TransactionAdded += new TransactionEventHandler(Portfolio_TransactionAdded);


	}
}