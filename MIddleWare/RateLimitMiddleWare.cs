using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestRabbitMQ.MIddleWare
{
    public class RateLimitMiddleWare : IMiddleware
    {
        private readonly int _maxRequest;
        private readonly TimeSpan _interval;
        private readonly ConcurrentDictionary<string, TokenBuckets> _bucket;

        public RateLimitMiddleWare(int maxRequest, TimeSpan interval)
        {
            _maxRequest = maxRequest;
            _interval = interval;
            _bucket = new ConcurrentDictionary<string, TokenBuckets>();
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            TokenBuckets tokenBucket;
            var ip = context.Connection.RemoteIpAddress.ToString();
            if (!_bucket.TryGetValue(ip, out tokenBucket)) {
                tokenBucket = new TokenBuckets(_maxRequest, _interval);
                _bucket.TryAdd(ip, tokenBucket);
            }
            if (!tokenBucket.TryTake())
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Too many requests. Please try again later.");
            }
            else {
                await next.Invoke(context);
            }
        }
    }
    public class TokenBuckets {
        private  int _tokens;
        private readonly int _capacity;
        private readonly TimeSpan _interval;
        private DateTime _lastFillTime;
        public TokenBuckets(int capacity,TimeSpan interval) {
            _tokens = capacity;
            _capacity = capacity;
            _interval = interval;
            _lastFillTime = DateTime.UtcNow;
        }
        public bool TryTake() {
            lock (this) {
                ReFill();
                if (_tokens>0) {
                    _tokens--;
                    return true;
                }
                return false;
            }
        }
        public void ReFill() {
            var now = DateTime.Now;
            var sinceLastTimeFill = now - _lastFillTime;
            var newTokens = (int)(sinceLastTimeFill.TotalSeconds*_capacity/_interval.TotalSeconds);
            _tokens = Math.Min(_tokens+newTokens,_capacity);
            _lastFillTime = now;
        }

    }
         
}
