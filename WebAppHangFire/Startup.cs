using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace WebAppHangFire
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddHangfire(configuration => configuration
                .UseRecommendedSerializerSettings()
                .UseSQLiteStorage());
            
            // Define a quantidade de retentativas aplicadas a um job com falha.
            // Por padr�o ser�o 10, aqui estamos abaixando para duas com intervalo de 5 minutos.
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
            {
                Attempts = 3,
                DelaysInSeconds = new int[] { 300 }
            });

            services.AddHangfireServer();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });

            app.UseHangfireDashboard();

            FireAndForgetJobExample();
            DelayedJobsExample();
            RecurringJobsExample();
            ContinuationJobExample();
            JobFailExample();

        }


        /// <summary>
        /// Jobs fire-and-forget s�o executados imediatamente e depois "esquecidos".
        /// Voc� ainda poder� voltar a execut�-los manualmente sempre que quiser
        /// gra�as ao Dashboard e ao hist�rico do Hangfire.
        /// </summary>
        public void FireAndForgetJobExample()
        {
            BackgroundJob.Enqueue(() => Console.WriteLine("Exemplo de job Fire-and-forget!"));
        }

        /// <summary>
        /// Jobs delayed s�o executados em uma data futura pr�-definida.
        /// </summary>
        public void DelayedJobsExample()
        {
            BackgroundJob.Schedule(
                () => Console.WriteLine("Exemplo de job Delayed executado 2 minutos ap�s o in�cio da aplica��o"),
                TimeSpan.FromMinutes(2));
        }

        /// <summary>
        /// Reurring jobs permitem que voc� agende a execu��o de jobs recorrentes utilizando uma nota��o Cron.
        /// </summary>
        public void RecurringJobsExample()
        {
            RecurringJob.AddOrUpdate(
                "Meu job recorrente",
                () => Console.WriteLine((new Random().Next(1, 200) % 2 == 0)
                    ? "Job recorrente gerou um n�mero par"
                    : "Job recorrente gerou um n�mero �mpar"),
                Cron.Minutely,
                TimeZoneInfo.Local);
        }

        /// <summary>
        /// Esta abordagem permite que voc� defina para a execu��o de um job iniciar
        /// apenas ap�s a conclus�o de um job pai.
        /// </summary>
        public void ContinuationJobExample()
        {
            var jobId = BackgroundJob.Enqueue(() => Console.WriteLine("Job fire-and-forget pai!"));
            BackgroundJob.ContinueJobWith(jobId, () => Console.WriteLine($"Job fire-and-forget filho! (Continua��o do job {jobId})"));
        }

        /// <summary>
        /// Quando um job falha o Hangfire ir� adiciona-la a fila para executar novamente.
        /// Por padr�o ele ir� tentar reexecutar 10 vezes, voc� pode alterar este comportamento
        /// adicionando a seguinte configura��o ao m�todo ConfigureServices:
        /// <code>GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = new int[] { 300 } });</code>
        /// </summary>
        public void JobFailExample()
        {
            BackgroundJob.Enqueue(() => FalharJob());
        }

        public void FalharJob()
        {
            throw new Exception("Deu ruim hein...");
        }
    }
}
