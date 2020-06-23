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
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Collection.Generic;
using NHibernate.Collection.Trackers;
using NHibernate.Engine;
using NHibernate.Impl;
using NHibernate.Loader;
using NHibernate.Persister.Collection;
using NHibernate.Proxy;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Collection
{
	public abstract partial class AbstractPersistentCollection : IPersistentCollection, ILazyInitializedCollection
	{

		protected virtual async Task<bool?> ReadKeyExistenceAsync<TKey, TValue>(TKey elementKey, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!initialized)
			{
				ThrowLazyInitializationExceptionIfNotConnected();
				CollectionEntry entry = session.PersistenceContext.GetCollectionEntry(this);
				ICollectionPersister persister = entry.LoadedPersister;
				if (persister.IsExtraLazy)
				{
					var queueOperationTracker = (AbstractMapQueueOperationTracker<TKey, TValue>) GetOrCreateQueueOperationTracker();
					if (queueOperationTracker == null)
					{
						if (HasQueuedOperations)
						{
							await (session.FlushAsync(cancellationToken)).ConfigureAwait(false);
						}

						return persister.IndexExists(entry.LoadedKey, elementKey, session);
					}

					if (queueOperationTracker.ContainsKey(elementKey))
					{
						return true;
					}

					if (queueOperationTracker.Cleared)
					{
						return false;
					}

					if (queueOperationTracker.IsElementKeyQueuedForDelete(elementKey))
					{
						return false;
					}

					// As keys are unordered we don't have to calculate the current order of the key
					return persister.IndexExists(entry.LoadedKey, elementKey, session);
				}
				Read();
			}
			return null;
		}

		internal async Task<bool> IsTransientAsync(object element, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var queryableCollection = (IQueryableCollection) Session.Factory.GetCollectionPersister(Role);
			return
				queryableCollection != null &&
				queryableCollection.ElementType.IsEntityType &&
				!element.IsProxy() &&
				!Session.PersistenceContext.IsEntryFor(element) &&
				await (ForeignKeys.IsTransientFastAsync(queryableCollection.ElementPersister.EntityName, element, Session, cancellationToken)).ConfigureAwait(false) == true;
		}

		/// <summary>
		/// Initialize the collection, if possible, wrapping any exceptions
		/// in a runtime exception
		/// </summary>
		/// <param name="writing">currently obsolete</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <exception cref="LazyInitializationException">if we cannot initialize</exception>
		protected virtual Task InitializeAsync(bool writing, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				if (!initialized)
				{
					if (initializing)
					{
						return Task.FromException<object>(new LazyInitializationException("illegal access to loading collection"));
					}
					ThrowLazyInitializationExceptionIfNotConnected();
					return session.InitializeCollectionAsync(this, writing, cancellationToken);
				}
				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		/// <summary>
		/// To be called internally by the session, forcing
		/// immediate initialization.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <remarks>
		/// This method is similar to <see cref="InitializeAsync(bool,CancellationToken)" />, except that different exceptions are thrown.
		/// </remarks>
		public virtual Task ForceInitializationAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			if (!initialized)
			{
				if (initializing)
				{
					return Task.FromException<object>(new AssertionFailure("force initialize loading collection"));
				}
				if (session == null)
				{
					return Task.FromException<object>(new HibernateException("collection is not associated with any session"));
				}
				if (!session.IsConnected)
				{
					return Task.FromException<object>(new HibernateException("disconnected session"));
				}
				return session.InitializeCollectionAsync(this, false, cancellationToken);
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// Called before inserting rows, to ensure that any surrogate keys are fully generated
		/// </summary>
		/// <param name="persister"></param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public virtual Task PreInsertAsync(ICollectionPersister persister, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				PreInsert(persister);
				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		// Since 5.3
		[Obsolete("This method has no more usages and will be removed in a future version")]
		public async Task IdentityRemoveAsync(IList list, object obj, string entityName, ISessionImplementor session, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (obj != null && await (ForeignKeys.IsNotTransientSlowAsync(entityName, obj, session, cancellationToken)).ConfigureAwait(false))
			{
				IType idType = session.Factory.GetEntityPersister(entityName).IdentifierType;

				object idOfCurrent = await (ForeignKeys.GetEntityIdentifierIfNotUnsavedAsync(entityName, obj, session, cancellationToken)).ConfigureAwait(false);
				List<object> toRemove = new List<object>(list.Count);
				foreach (object current in list)
				{
					if (current == null)
					{
						continue;
					}
					object idOfOld = await (ForeignKeys.GetEntityIdentifierIfNotUnsavedAsync(entityName, current, session, cancellationToken)).ConfigureAwait(false);
					if (idType.IsEqual(idOfCurrent, idOfOld, session.Factory))
					{
						toRemove.Add(current);
					}
				}
				foreach (object ro in toRemove)
				{
					list.Remove(ro);
				}
			}
		}

		/// <summary>
		/// Disassemble the collection, ready for the cache
		/// </summary>
		/// <param name="persister"></param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns></returns>
		public abstract Task<object> DisassembleAsync(ICollectionPersister persister, CancellationToken cancellationToken);

		/// <summary>
		/// Get all the elements that need deleting
		/// </summary>
		public abstract Task<IEnumerable> GetDeletesAsync(ICollectionPersister persister, bool indexIsFormula, CancellationToken cancellationToken);

		public abstract Task<bool> EqualsSnapshotAsync(ICollectionPersister persister, CancellationToken cancellationToken);

		/// <summary>
		/// Read the state of the collection from a disassembled cached value.
		/// </summary>
		/// <param name="persister"></param>
		/// <param name="disassembled"></param>
		/// <param name="owner"></param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public abstract Task InitializeFromCacheAsync(ICollectionPersister persister, object disassembled, object owner, CancellationToken cancellationToken);

		/// <summary>
		/// Do we need to update this element?
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="i"></param>
		/// <param name="elemType"></param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns></returns>
		public abstract Task<bool> NeedsUpdatingAsync(object entry, int i, IType elemType, CancellationToken cancellationToken);

		/// <summary>
		/// Reads the row from the <see cref="DbDataReader"/>.
		/// </summary>
		/// <param name="reader">The DbDataReader that contains the value of the Identifier</param>
		/// <param name="role">The persister for this Collection.</param>
		/// <param name="descriptor">The descriptor providing result set column names</param>
		/// <param name="owner">The owner of this Collection.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns>The object that was contained in the row.</returns>
		public abstract Task<object> ReadFromAsync(DbDataReader reader, ICollectionPersister role, ICollectionAliases descriptor,
										object owner, CancellationToken cancellationToken);

		/// <summary>
		/// Do we need to insert this element?
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="i"></param>
		/// <param name="elemType"></param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns></returns>
		public abstract Task<bool> NeedsInsertingAsync(object entry, int i, IType elemType, CancellationToken cancellationToken);
	}
}
