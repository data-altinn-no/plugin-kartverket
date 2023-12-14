using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dan.Plugin.Kartverket.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class RegisterenhetsRettsandelsServiceClientService : IRegisterenhetsRettsandelsServiceClientService
    {
        private readonly ILogger _logger;
        private readonly ApplicationSettings _settings;

        public RegisterenhetsRettsandelsServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<RegisterenhetsRettsandelsServiceClientService>();
        }
    }

    public interface IRegisterenhetsRettsandelsServiceClientService
    {

    }
}
