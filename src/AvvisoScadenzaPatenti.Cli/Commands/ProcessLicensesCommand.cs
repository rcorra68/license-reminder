namespace AvvisoScadenzaPatenti.Cli.Commands;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Services;
using Serilog;

public class ProcessLicensesCommand : ILicenseCommand
{
    private readonly IEmailService _emailService;
    private readonly LicenseOrchestrator _orchestrator;

    public ProcessLicensesCommand(IEmailService emailService, LicenseOrchestrator orchestrator)
    {
        _emailService = emailService;
        _orchestrator = orchestrator;
    }

    public async Task<int> ExecuteAsync(Options opts, CancellationToken ct)
    {
        if (!await _emailService.VerifyEmailConnectivityAsync(ct))
        {
            Log.Warning("SMTP Health Check failed. Licenses will be processed, but notifications might not be delivered.");
        }

        await _orchestrator.ProcessLicensesAsync(ct);

        return 0;
    }
}