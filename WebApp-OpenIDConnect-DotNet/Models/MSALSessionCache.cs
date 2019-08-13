using Microsoft.Identity.Client;

using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace WebApp_OpenIDConnect_DotNet.Models
{
    public class MSALSessionCache
    {
        private static ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        string UserId = string.Empty;
        string CacheId = string.Empty;
        private readonly HttpContext httpContext = null;
        private ITokenCache cache;

        public MSALSessionCache(string userId, HttpContext httpcontext)
        {
            // not object, we want the SUB
            UserId = userId;
            CacheId = UserId + "_TokenCache";
            httpContext = httpcontext;
        }

        public ITokenCache EnablePersistence(ITokenCache cache)
        {
            this.cache = cache;
            cache.SetBeforeAccess(BeforeAccessNotification);
            cache.SetAfterAccess(AfterAccessNotification);
            return cache;
        }

        public void SaveUserStateValue(string state)
        {
            SessionLock.EnterWriteLock();
            httpContext.Session.SetString(CacheId + "_state", state);
            SessionLock.ExitWriteLock();
        }
        public string ReadUserStateValue()
        {
            string state = string.Empty;
            SessionLock.EnterReadLock();
            state = (string)httpContext.Session.GetString(CacheId + "_state");
            SessionLock.ExitReadLock();
            return state;
        }
        public void Load(TokenCacheNotificationArgs args)
        {
            SessionLock.EnterReadLock();
            byte[] blob = httpContext.Session.Get(CacheId);
            if(blob != null)
            {
                args.TokenCache.DeserializeMsalV3(blob);
            }
            SessionLock.ExitReadLock();
        }

        public void Persist(TokenCacheNotificationArgs args)
        {
            SessionLock.EnterWriteLock();

            // Reflect changes in the persistent store
            httpContext.Session.Set(CacheId, args.TokenCache.SerializeMsalV3());
            SessionLock.ExitWriteLock();
        }

        // Triggered right before MSAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load(args);
        }

        // Triggered right after MSAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                Persist(args);
            }
        }
    }
}