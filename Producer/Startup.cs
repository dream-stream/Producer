using System;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Producer.Serialization;
using Producer.Services;

namespace Producer
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;

        public Startup(IWebHostEnvironment env)
        {
            _env = env;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ISerializer, Serializer>();
            services.AddTransient<ClientWebSocket>();

            if (_env.IsDevelopment())
            {
                services.AddHostedService<WebSocketService1>();
                services.AddHostedService<WebSocketService2>();
            }
            else
            {
                var tryParse = int.TryParse(Environment.GetEnvironmentVariable("test"), out var test);
                if(!tryParse) throw new Exception("Please set the env \"test\" to a int between 1 and 3");
                services.AddHostedService<WebSocketService3>();
                if(test > 1)
                    services.AddHostedService<WebSocketService4>();
                if (test > 2)
                    services.AddHostedService<WebSocketService5>();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
        }
    }
}
