﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Action;
using NHibernate.Cache;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Engine
{
	public partial class ActionQueue
	{

		private async Task ExecuteActionsAsync<T>(List<T> list, CancellationToken cancellationToken) where T: IExecutable
		{
			cancellationToken.ThrowIfCancellationRequested();
			// Actions may raise events to which user code can react and cause changes to action list.
			// It will then fail here due to list being modified. (Some previous code was dodging the
			// trouble with a for loop which was not failing provided the list was not getting smaller.
			// But then it was clearing it without having executed added actions (if any), ...)
			foreach (var executable in list)
			{
				await (InnerExecuteAsync(executable, cancellationToken)).ConfigureAwait(false);
			}
			list.Clear();
			await (session.Batcher.ExecuteBatchAsync(cancellationToken)).ConfigureAwait(false);
		}

		private Task PreInvalidateCachesAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			if (session.Factory.Settings.IsQueryCacheEnabled)
			{
				return session.Factory.UpdateTimestampsCache.PreInvalidateAsync(executedSpaces, cancellationToken);
			}
			return Task.CompletedTask;
		}

		public async Task ExecuteAsync(IExecutable executable, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				await (InnerExecuteAsync(executable, cancellationToken)).ConfigureAwait(false);
			}
			finally
			{
				await (PreInvalidateCachesAsync(cancellationToken)).ConfigureAwait(false);
			}
		}

		private async Task InnerExecuteAsync(IExecutable executable, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				await (executable.ExecuteAsync(cancellationToken)).ConfigureAwait(false);
			}
			finally
			{
				RegisterCleanupActions(executable);
			}
		}

		/// <summary> 
		/// Perform all currently queued entity-insertion actions.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public async Task ExecuteInsertsAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				await (ExecuteActionsAsync(insertions, cancellationToken)).ConfigureAwait(false);
			}
			finally
			{
				await (PreInvalidateCachesAsync(cancellationToken)).ConfigureAwait(false);
			}
		}

		/// <summary> 
		/// Perform all currently queued actions. 
		/// </summary>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public async Task ExecuteActionsAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				await (ExecuteActionsAsync(insertions, cancellationToken)).ConfigureAwait(false);
				await (ExecuteActionsAsync(updates, cancellationToken)).ConfigureAwait(false);
				await (ExecuteActionsAsync(collectionRemovals, cancellationToken)).ConfigureAwait(false);
				await (ExecuteActionsAsync(collectionUpdates, cancellationToken)).ConfigureAwait(false);
				await (ExecuteActionsAsync(collectionCreations, cancellationToken)).ConfigureAwait(false);
				await (ExecuteActionsAsync(deletions, cancellationToken)).ConfigureAwait(false);
			}
			finally
			{
				await (PreInvalidateCachesAsync(cancellationToken)).ConfigureAwait(false);
			}
		}

		private static async Task PrepareActionsAsync<T>(List<T> queue, CancellationToken cancellationToken) where T: IExecutable
		{
			cancellationToken.ThrowIfCancellationRequested();
			foreach (var executable in queue)
				await (executable.BeforeExecutionsAsync(cancellationToken)).ConfigureAwait(false);
		}

		/// <summary>
		/// Prepares the internal action queues for execution.  
		/// </summary>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public async Task PrepareActionsAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await (PrepareActionsAsync(collectionRemovals, cancellationToken)).ConfigureAwait(false);
			await (PrepareActionsAsync(collectionUpdates, cancellationToken)).ConfigureAwait(false);
			await (PrepareActionsAsync(collectionCreations, cancellationToken)).ConfigureAwait(false);
		}

		/// <summary>
		/// Execute any registered <see cref="IBeforeTransactionCompletionProcess" />
		/// </summary>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public Task BeforeTransactionCompletionAsync(CancellationToken cancellationToken) 
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			return beforeTransactionProcesses.BeforeTransactionCompletionAsync(cancellationToken);
		}

		/// <summary> 
		/// Performs cleanup of any held cache softlocks.
		/// </summary>
		/// <param name="success">Was the transaction successful.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public async Task AfterTransactionCompletionAsync(bool success, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await (afterTransactionProcesses.AfterTransactionCompletionAsync(success, cancellationToken)).ConfigureAwait(false);

			await (InvalidateCachesAsync(cancellationToken)).ConfigureAwait(false);
		}

		private async Task InvalidateCachesAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (session.Factory.Settings.IsQueryCacheEnabled)
			{
				await (session.Factory.UpdateTimestampsCache.InvalidateAsync(executedSpaces, cancellationToken)).ConfigureAwait(false);
			}

			executedSpaces.Clear();
		}
		private partial class BeforeTransactionCompletionProcessQueue 
		{
	
			public async Task BeforeTransactionCompletionAsync(CancellationToken cancellationToken) 
			{
				cancellationToken.ThrowIfCancellationRequested();
				int size = processes.Count;
				for (int i = 0; i < size; i++)
				{
					try 
					{
						var process = processes[i];
						await (process.ExecuteBeforeTransactionCompletionAsync(cancellationToken)).ConfigureAwait(false);
					}
					catch (OperationCanceledException) { throw; }
					catch (HibernateException)
					{
						throw;
					}
					catch (Exception e) 
					{
						throw new AssertionFailure("Unable to perform BeforeTransactionCompletion callback", e);
					}
				}
				processes.Clear();
			}
		}
		private partial class AfterTransactionCompletionProcessQueue 
		{
	
			public async Task AfterTransactionCompletionAsync(bool success, CancellationToken cancellationToken) 
			{
				cancellationToken.ThrowIfCancellationRequested();
				int size = processes.Count;
				
				for (int i = 0; i < size; i++)
				{
					try
					{
						var process = processes[i];
						await (process.ExecuteAfterTransactionCompletionAsync(success, cancellationToken)).ConfigureAwait(false);
					}
					catch (OperationCanceledException) { throw; }
					catch (CacheException e)
					{
						log.Error(e, "could not release a cache lock");
						// continue loop
					}
					catch (Exception e)
					{
						throw new AssertionFailure("Unable to perform AfterTransactionCompletion callback", e);
					}
				}
				processes.Clear();
			}
		}
		private partial class BeforeTransactionCompletionDelegatedProcess : IBeforeTransactionCompletionProcess
		{

			public Task ExecuteBeforeTransactionCompletionAsync(CancellationToken cancellationToken)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return Task.FromCanceled<object>(cancellationToken);
				}
				try
				{
					ExecuteBeforeTransactionCompletion();
					return Task.CompletedTask;
				}
				catch (Exception ex)
				{
					return Task.FromException<object>(ex);
				}
			}
		}
		private partial class AfterTransactionCompletionDelegatedProcess : IAfterTransactionCompletionProcess
		{

			public Task ExecuteAfterTransactionCompletionAsync(bool success, CancellationToken cancellationToken)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return Task.FromCanceled<object>(cancellationToken);
				}
				try
				{
					ExecuteAfterTransactionCompletion(success);
					return Task.CompletedTask;
				}
				catch (Exception ex)
				{
					return Task.FromException<object>(ex);
				}
			}
		}
	}
}
