﻿using System;
using JetBrains.Annotations;
using NewRelic.Agent.Extensions.Providers.Wrapper;

namespace NewRelic.Agent.Extensions.Parsing
{
	public class ParsedSqlStatement
	{

		public DatastoreVendor DatastoreVendor { get; }

		/// <summary>
		/// The "direct object", eg what the operation is operating on.
		/// </summary>
		public string Model { get; }

		/// <summary>
		/// The operation the data base is performing.
		/// </summary>
		public string Operation { get; }

		public string DatastoreStatementMetricName { get; }

		/// <summary>
		/// Construct a summarized SQL statement.
		/// 
		/// Examples:
		///   select * from dude ==> ParsedDatabaseStatement("dude", "select");
		///   set @foo=17 ==> ParsedDatabaseStatement("foo", "set")
		///  
		/// See DatabaseStatementParserTest for additional examples.
		/// 
		/// </summary>
		/// <param name="model">What the statement is operating on, eg the "direct object" of the operation.</param>
		/// <param name="operation">What the operation is doing.</param>
		public ParsedSqlStatement([NotNull] DatastoreVendor datastoreVendor, [CanBeNull] string model, [NotNull] string operation)
		{
			Model = model;
			Operation = operation ?? "other";
			DatastoreVendor = datastoreVendor;
			DatastoreStatementMetricName = String.Join("/", new string[] { "Datastore/statement", datastoreVendor.ToString(), Model, Operation });
		}

		public static ParsedSqlStatement FromOperation(DatastoreVendor vendor, string operation)
		{
			return new ParsedSqlStatement(vendor, null, operation);
		}

		public override string ToString()
		{
			return Model + '/' + Operation;
		}
	}
}
