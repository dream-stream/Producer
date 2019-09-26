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
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ISerializer, Serializer>();
            services.AddTransient<ClientWebSocket>();

            services.AddHostedService<WebSocketService1>();
            services.AddHostedService<WebSocketService2>();
            //services.AddHostedService<WebSocketService3>();
            //services.AddHostedService<WebSocketService4>();
            //services.AddHostedService<WebSocketService5>();
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
