﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Data.Common;
using NHibernate.Cfg;
using NHibernate.Util;
using Environment=NHibernate.Cfg.Environment;
using NHibernate.AdoNet.Util;

namespace NHibernate.Tool.hbm2ddl
{
	using System.Threading.Tasks;
	using System.Threading;
	public partial class SchemaUpdate
	{

		private async Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (wasInitialized)
			{
				return;
			}

			string autoKeyWordsImport = PropertiesHelper.GetString(Environment.Hbm2ddlKeyWords, configuration.Properties, "not-defined");
			if (autoKeyWordsImport == Hbm2DDLKeyWords.AutoQuote)
			{
				await (SchemaMetadataUpdater.UpdateAsync(configuration, dialect, cancellationToken)).ConfigureAwait(false);
				SchemaMetadataUpdater.QuoteTableAndColumns(configuration, dialect);
			}

			wasInitialized = true;
		}

		public static async Task MainAsync(string[] args, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				var cfg = new Configuration();

				bool script = true;
				// If true then execute db updates, otherwise just generate and display updates
				bool doUpdate = true;
				//String propFile = null;

				for (int i = 0; i < args.Length; i++)
				{
					if (args[i].StartsWith("--", StringComparison.Ordinal))
					{
						if (args[i].Equals("--quiet"))
						{
							script = false;
						}
						else if (args[i].StartsWith("--properties=", StringComparison.Ordinal))
						{
							throw new NotSupportedException("No properties file for .NET, use app.config instead");
							//propFile = args[i].Substring( 13 );
						}
						else if (args[i].StartsWith("--config=", StringComparison.Ordinal))
						{
							cfg.Configure(args[i].Substring(9));
						}
						else if (args[i].StartsWith("--text", StringComparison.Ordinal))
						{
							doUpdate = false;
						}
						else if (args[i].StartsWith("--naming=", StringComparison.Ordinal))
						{
							cfg.SetNamingStrategy(
								(INamingStrategy)
								Environment.ServiceProvider.GetInstance(ReflectHelper.ClassForName(args[i].Substring(9))));
						}
					}
					else
					{
						cfg.AddFile(args[i]);
					}
				}

				/* NH: No props file for .NET
				 * if ( propFile != null ) {
					Hashtable props = new Hashtable();
					props.putAll( cfg.Properties );
					props.load( new FileInputStream( propFile ) );
					cfg.SetProperties( props );
				}*/

				await (new SchemaUpdate(cfg).ExecuteAsync(script, doUpdate, cancellationToken)).ConfigureAwait(false);
			}
			catch (OperationCanceledException) { throw; }
			catch (Exception e)
			{
				log.Error(e, "Error running schema update");
				Console.WriteLine(e);
			}
		}

		/// <summary>
		/// Execute the schema updates
		/// </summary>
		public Task ExecuteAsync(bool useStdOut, bool doUpdate, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				if (useStdOut)
				{
					return ExecuteAsync(Console.WriteLine, doUpdate, cancellationToken);
				}
				else
				{
					return ExecuteAsync(null, doUpdate, cancellationToken);
				}
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		/// <summary>
		/// Execute the schema updates
		/// </summary>
		/// <param name="scriptAction">The action to write the each schema line.</param>
		/// <param name="doUpdate">Commit the script to DB</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public async Task ExecuteAsync(Action<string> scriptAction, bool doUpdate, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			log.Info("Running hbm2ddl schema update");

			await (InitializeAsync(cancellationToken)).ConfigureAwait(false);

			DbConnection connection;
			DbCommand stmt = null;

			exceptions.Clear();

			try
			{
				DatabaseMetadata meta;
				try
				{
					log.Info("fetching database metadata");
					await (connectionHelper.PrepareAsync(cancellationToken)).ConfigureAwait(false);
					connection = connectionHelper.Connection;
					meta = new DatabaseMetadata(connection, dialect);
					stmt = connection.CreateCommand();
				}
				catch (OperationCanceledException) { throw; }
				catch (Exception sqle)
				{
					exceptions.Add(sqle);
					log.Error(sqle, "could not get database metadata");
					throw;
				}

				log.Info("updating schema");

				string[] createSQL = configuration.GenerateSchemaUpdateScript(dialect, meta);
				for (int j = 0; j < createSQL.Length; j++)
				{
					string sql = createSQL[j];
					string formatted = formatter.Format(sql);

					try
					{
						if (scriptAction != null)
						{
							scriptAction(formatted);
						}
						if (doUpdate)
						{
							log.Debug(sql);
							stmt.CommandText = sql;
							await (stmt.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
						}
					}
					catch (OperationCanceledException) { throw; }
					catch (Exception e)
					{
						exceptions.Add(e);
						log.Error(e, "Unsuccessful: {0}", sql);
					}
				}

				log.Info("schema update complete");
			}
			catch (OperationCanceledException) { throw; }
			catch (Exception e)
			{
				exceptions.Add(e);
				log.Error(e, "could not complete schema update");
			}
			finally
			{
				try
				{
					if (stmt != null)
					{
						stmt.Dispose();
					}
					connectionHelper.Release();
				}
				catch (Exception e)
				{
					exceptions.Add(e);
					log.Error(e, "Error closing connection");
				}
			}
		}
	}
}
