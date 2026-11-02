using GreenCrescent.Application.Common;
using Microsoft.Extensions.Configuration;
using Resend;

namespace GreenCrescent.Infrastructure.Services;

public sealed class ResendOrphanApplicationEmailService : IOrphanApplicationEmailService
{
    private readonly IResend _resend;
    private readonly IConfiguration _configuration;

    public ResendOrphanApplicationEmailService(IResend resend, IConfiguration configuration)
    {
        _resend = resend;
        _configuration = configuration;
    }

    public async Task SendDecisionEmailAsync(
        string? toEmail,
        string applicantName,
        string orphanFullName,
        string trackingCode,
        bool isApproved,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return;
        }

        var fromEmail = _configuration["Resend:FromEmail"]
            ?? throw new InvalidOperationException("Resend FromEmail is missing.");

        var subject = isApproved
            ? $"قبول طلب كفالة اليتيم ({orphanFullName}) - جمعية الهلال الأخضر"
            : $"تحديث بخصوص طلب كفالة اليتيم ({orphanFullName}) - جمعية الهلال الأخضر";

        var statusHtml = isApproved
            ? "<p style='color:#15803d;font-weight:bold;'>يسرنا إبلاغكم بأنه قد تمت الموافقة على طلب كفالة اليتيم, يرجى احضار الوثائق المطلوبة والتوجه الى فرع اربد.</p>"
            : "<p style='color:#b91c1c;font-weight:bold;'>نعتذر منكم، لم يتم قبول طلب كفالة اليتيم.</p>";

        var reasonHtml = !string.IsNullOrWhiteSpace(reason)
            ? $"<p><strong>الملاحظات / السبب:</strong> {reason}</p>"
            : string.Empty;

        var message = new EmailMessage
        {
            From = $"جمعية الهلال الأخضر <{fromEmail}>",
            Subject = subject,
            HtmlBody = $"""
                <div dir="rtl" style="font-family: Tahoma, Arial, sans-serif; line-height: 1.8; color: #1f2937;">
                    <h2>جمعية الهلال الأخضر الخيرية</h2>
                    <p>الأخ/الأخت <strong>{applicantName}</strong> المحترم/ة،</p>
                    {statusHtml}
                    {reasonHtml}
                    <p><strong>اسم اليتيم:</strong> {orphanFullName}</p>
                    <p><strong>رقم التتبع الخاص بالطلب:</strong> {trackingCode}</p>
                    <hr style="border:none;border-top:1px solid #e5e7eb;margin:20px 0;" />
                    <small style="color:#6b7280;">هذه رسالة تلقائية من نظام جمعية الهلال الأخضر الخيرية.</small>
                </div>
                """
        };

        message.To.Add(toEmail.Trim());

        await _resend.EmailSendAsync(message);
    }
}