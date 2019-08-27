using Microsoft.Identity.Client;

using System.Threading;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace WebApp_OpenIDConnect_DotNet.Models
{
    /// <summary>
    /// This implementation is just for demo purposes and does not scale. For better cache implementations see 
    /// https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/2-WebApp-graph-user/2-1-Call-MSGraph
    /// </summary>
    public class MSALStaticCache
    {
        private static Dictionary<string, byte[]> staticCache = new Dictionary<string, byte[]>();

        private static ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly string userId = string.Empty;
        private readonly string cacheId = string.Empty;
        private readonly HttpContext httpContext = null;
        private ITokenCache cache;

        public MSALStaticCache(string userId, HttpContext httpcontext)
        {
            // not object, we want the SUB
            this.userId = userId;
            cacheId = this.userId + "_TokenCache";
            httpContext = httpcontext;
        }

        public ITokenCache EnablePersistence(ITokenCache cache)
        {
            this.cache = cache;
            cache.SetBeforeAccess(BeforeAccessNotification);
            cache.SetAfterAccess(AfterAccessNotification);
            return cache;
        }

        public void Load(TokenCacheNotificationArgs args)
        {
            SessionLock.EnterReadLock();
            byte[] blob = staticCache.ContainsKey(cacheId) ? staticCache[cacheId] : null ;
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
            staticCache[cacheId] = args.TokenCache.SerializeMsalV3();
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