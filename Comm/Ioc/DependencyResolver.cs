using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Ioc
{
    public class DependencyResolver
    {
        private static readonly DependencyResolver _instance = new DependencyResolver();
        private static IServiceCollection _services = new ServiceCollection();

        #region Singleton

        public static DependencyResolver Instance
        {
            get { return _instance; }
        }
        public static IServiceCollection Services
        {
            get { return _services; }
        }
        #endregion

        #region Construction
        private DependencyResolver()
        {
            if (_services == null)
                _services = new ServiceCollection();
        }
        #endregion

        #region Public Methods
        public T GetService<T>()
        {
            var t = _services.BuildServiceProvider().GetService<T>();
            return t;
        }
        public void AddScoped(Type serviceType, Type implementType)
        {
            if (_services != null)
            {
                _services.AddScoped(serviceType, implementType);
            }
        }
        public void AddSingleton(Type serviceType, Type implementType)
        {
            if (_services != null)
            {
                _services.AddSingleton(serviceType, implementType);
            }
        }
        public void AddScoped<TService, TImplement>() where TService : class where TImplement : class, TService
        {
            if (_services != null)
            {
                _services.AddScoped<TService, TImplement>();
            }
        }
        public void AddSingleton<TService, TImplement>() where TService : class where TImplement : class, TService
        {
            if (_services != null)
            {
                _services.AddSingleton<TService, TImplement>();
            }
        }
        
        #endregion
    }
}
