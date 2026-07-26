using Antital.Domain.Configuration;
using Antital.Domain.Interfaces;
using BuildingBlocks.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Antital.API.Controllers;

/// <summary>
/// Development-only Dojah sandbox probe. Unauthenticated so keys can be smoke-tested without login.
/// Hard-gated to Development environment — returns 404 elsewhere.
/// </summary>
[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public class DojahSandboxController(
    IDojahClient dojahClient,
    IOptions<DojahSettings> dojahOptions,
    IHostEnvironment environment
) : BaseController
{
    private const string SandboxTestBvn = "22222222222";

    public sealed record LivenessProbeRequest(string? Image);

    [HttpGet("/dev/dojah")]
    public IActionResult Page()
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var settings = dojahOptions.Value;
        var appIdJson = System.Text.Json.JsonSerializer.Serialize(settings.AppId ?? string.Empty);
        var publicKeyJson = System.Text.Json.JsonSerializer.Serialize(settings.PublicKey ?? string.Empty);
        var widgetIdJson = System.Text.Json.JsonSerializer.Serialize(settings.WidgetId ?? string.Empty);
        var referenceIdJson = System.Text.Json.JsonSerializer.Serialize($"ANTITAL-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");

        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>Dojah Sandbox Probe — Antital</title>
              <style>
                :root { color-scheme: light; font-family: ui-sans-serif, system-ui, sans-serif; }
                body { margin: 0; background: #f6f7f4; color: #111; }
                main { max-width: 760px; margin: 48px auto; padding: 0 20px 48px; }
                h1 { font-size: 1.5rem; margin: 0 0 8px; }
                h2 { font-size: 1.05rem; margin: 0 0 12px; }
                p { color: #555; line-height: 1.5; }
                .card { background: #fff; border: 1px solid #e5e5e5; border-radius: 12px; padding: 20px; margin-top: 20px; }
                label { display: block; font-size: 0.875rem; margin-bottom: 6px; }
                input[type=text], input[type=file] { width: 100%; box-sizing: border-box; padding: 12px; border: 1px solid #ccc; border-radius: 8px; font-size: 1rem; }
                .row { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 12px; }
                button { padding: 12px 18px; border: 0; border-radius: 8px; background: #042E27; color: #fff; font-weight: 600; cursor: pointer; }
                button.secondary { background: #4379B7; }
                button:disabled { opacity: 0.6; cursor: wait; }
                .meta { font-size: 0.85rem; color: #666; margin-top: 12px; }
                pre { background: #0f172a; color: #e2e8f0; padding: 16px; border-radius: 10px; overflow: auto; font-size: 0.8rem; min-height: 120px; max-height: 420px; }
                .ok { color: #166534; }
                .bad { color: #b91c1c; }
                code { background: #eee; padding: 2px 6px; border-radius: 4px; }
                video, img.preview { width: 100%; max-height: 280px; border-radius: 10px; background: #111; object-fit: cover; }
                .hint { font-size: 0.85rem; color: #666; margin-top: 8px; }
                #embed-container { width: 100%; max-width: 520px; height: 720px; margin-top: 12px; border: 1px solid #e5e5e5; border-radius: 12px; overflow: hidden; background: #fff; }
              </style>
            </head>
            <body>
              <main>
                <h1>Dojah sandbox probe</h1>
                <p>Unauthenticated Development-only page. Widget SDK + raw API probes against Dojah sandbox.</p>

                <div class="card">
                  <div class="meta">
                    BaseUrl: <code>{{settings.BaseUrl}}</code><br />
                    AppId: <strong>{{(string.IsNullOrWhiteSpace(settings.AppId) ? "missing" : "set")}}</strong> ·
                    PublicKey: <strong>{{(string.IsNullOrWhiteSpace(settings.PublicKey) ? "missing" : "set")}}</strong> ·
                    WidgetId: <strong>{{(string.IsNullOrWhiteSpace(settings.WidgetId) ? "missing" : settings.WidgetId)}}</strong> ·
                    PrivateKey: <strong>{{(string.IsNullOrWhiteSpace(settings.PrivateKey) ? "missing" : "set")}}</strong>
                  </div>
                </div>

                <div class="card">
                  <h2>0. EasyOnboard widget (JS SDK)</h2>
                  <p class="hint">
                    Uses <code>https://widget.dojah.io/widget.js</code> with your EasyOnboard flow
                    (<a href="https://identity.dojah.io?widget_id={{settings.WidgetId}}" target="_blank" rel="noreferrer">open flow link</a>).
                    Client callbacks are for UX only — production should confirm via backend + reference id.
                  </p>
                  <div class="row">
                    <button id="openWidget" type="button">Open widget (modal)</button>
                    <button id="embedWidget" class="secondary" type="button">Embed widget inline</button>
                  </div>
                  <div id="embed-container"></div>
                </div>

                <div class="card">
                  <h2>1. BVN lookup (API)</h2>
                  <label for="bvn">BVN (sandbox test default <code>{{SandboxTestBvn}}</code>)</label>
                  <input id="bvn" type="text" value="{{SandboxTestBvn}}" maxlength="11" inputmode="numeric" />
                  <div class="row">
                    <button id="runBvn" type="button">Lookup BVN</button>
                  </div>
                </div>

                <div class="card">
                  <h2>2. Liveness check (API)</h2>
                  <p class="hint">Uses Dojah <code>POST /api/v1/ml/liveness/</code>. Capture from camera or upload a selfie.</p>
                  <video id="cam" autoplay playsinline muted></video>
                  <img id="shot" class="preview" alt="Captured selfie preview" hidden />
                  <div class="row">
                    <button id="startCam" class="secondary" type="button">Start camera</button>
                    <button id="capture" type="button" disabled>Capture selfie</button>
                    <button id="runLiveness" type="button" disabled>Run liveness</button>
                  </div>
                  <label for="file" style="margin-top:16px">Or upload an image</label>
                  <input id="file" type="file" accept="image/*" />
                </div>

                <div class="card">
                  <p id="status">Ready.</p>
                  <pre id="out">{}</pre>
                </div>
              </main>
              <script src="https://widget.dojah.io/widget.js"></script>
              <script>
                const statusEl = document.getElementById('status');
                const outEl = document.getElementById('out');
                const bvnEl = document.getElementById('bvn');
                const cam = document.getElementById('cam');
                const shot = document.getElementById('shot');
                const startCamBtn = document.getElementById('startCam');
                const captureBtn = document.getElementById('capture');
                const runLivenessBtn = document.getElementById('runLiveness');
                const fileEl = document.getElementById('file');
                let imageBase64 = null;
                let stream = null;

                const dojahConfig = {
                  app_id: {{appIdJson}},
                  p_key: {{publicKeyJson}},
                  widget_id: {{widgetIdJson}},
                  reference_id: {{referenceIdJson}}
                };

                function setStatus(text, ok) {
                  statusEl.textContent = text;
                  statusEl.className = ok === true ? 'ok' : (ok === false ? 'bad' : '');
                }

                function setImage(dataUrl) {
                  imageBase64 = dataUrl;
                  shot.src = dataUrl;
                  shot.hidden = false;
                  runLivenessBtn.disabled = false;
                }

                async function showJson(res) {
                  const text = await res.text();
                  let pretty = text;
                  try { pretty = JSON.stringify(JSON.parse(text), null, 2); } catch {}
                  outEl.textContent = pretty;
                  setStatus(res.ok ? ('OK · HTTP ' + res.status) : ('Failed · HTTP ' + res.status), res.ok);
                }

                function showPayload(label, payload, ok) {
                  outEl.textContent = JSON.stringify(payload, null, 2);
                  setStatus(label, ok);
                }

                function buildWidgetOptions(embed) {
                  if (!dojahConfig.app_id || !dojahConfig.p_key || !dojahConfig.widget_id) {
                    throw new Error('Missing Dojah AppId, PublicKey, or WidgetId in appsettings.');
                  }
                  if (typeof Connect === 'undefined') {
                    throw new Error('Dojah widget.js failed to load.');
                  }

                  const options = {
                    app_id: dojahConfig.app_id,
                    p_key: dojahConfig.p_key,
                    type: 'custom',
                    reference_id: dojahConfig.reference_id + '-' + Date.now().toString().slice(-4),
                    config: { widget_id: dojahConfig.widget_id },
                    metadata: { source: 'antital-dev-probe' },
                    onSuccess: function (response) {
                      showPayload('Widget onSuccess (confirm via backend in production)', response, true);
                    },
                    onError: function (err) {
                      showPayload('Widget onError', err, false);
                    },
                    onClose: function () {
                      setStatus('Widget closed / abandoned', false);
                    }
                  };

                  if (embed) {
                    options.embed = true;
                    options.container = '#embed-container';
                  }

                  return options;
                }

                document.getElementById('openWidget').addEventListener('click', () => {
                  try {
                    const connect = new Connect(buildWidgetOptions(false));
                    connect.setup();
                    connect.open();
                    setStatus('Widget modal opened.');
                  } catch (err) {
                    setStatus(String(err), false);
                    outEl.textContent = String(err);
                  }
                });

                document.getElementById('embedWidget').addEventListener('click', () => {
                  try {
                    document.getElementById('embed-container').innerHTML = '';
                    const connect = new Connect(buildWidgetOptions(true));
                    connect.setup();
                    connect.open();
                    setStatus('Widget embedded inline.');
                  } catch (err) {
                    setStatus(String(err), false);
                    outEl.textContent = String(err);
                  }
                });

                document.getElementById('runBvn').addEventListener('click', async () => {
                  const bvn = bvnEl.value.trim();
                  setStatus('Calling /api/dev/dojah/bvn …');
                  try {
                    const res = await fetch('/api/dev/dojah/bvn?bvn=' + encodeURIComponent(bvn));
                    await showJson(res);
                  } catch (err) {
                    setStatus('Request error', false);
                    outEl.textContent = String(err);
                  }
                });

                startCamBtn.addEventListener('click', async () => {
                  try {
                    if (stream) stream.getTracks().forEach(t => t.stop());
                    stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'user' }, audio: false });
                    cam.srcObject = stream;
                    captureBtn.disabled = false;
                    setStatus('Camera ready — capture a selfie.');
                  } catch (err) {
                    setStatus('Camera permission failed — use file upload instead.', false);
                    outEl.textContent = String(err);
                  }
                });

                captureBtn.addEventListener('click', () => {
                  const canvas = document.createElement('canvas');
                  canvas.width = cam.videoWidth || 640;
                  canvas.height = cam.videoHeight || 480;
                  canvas.getContext('2d').drawImage(cam, 0, 0, canvas.width, canvas.height);
                  setImage(canvas.toDataURL('image/jpeg', 0.92));
                  setStatus('Selfie captured — run liveness.');
                });

                fileEl.addEventListener('change', () => {
                  const file = fileEl.files && fileEl.files[0];
                  if (!file) return;
                  const reader = new FileReader();
                  reader.onload = () => {
                    setImage(String(reader.result));
                    setStatus('Image loaded — run liveness.');
                  };
                  reader.readAsDataURL(file);
                });

                runLivenessBtn.addEventListener('click', async () => {
                  if (!imageBase64) {
                    setStatus('Capture or upload an image first.', false);
                    return;
                  }
                  runLivenessBtn.disabled = true;
                  setStatus('Calling /api/dev/dojah/liveness …');
                  try {
                    const res = await fetch('/api/dev/dojah/liveness', {
                      method: 'POST',
                      headers: { 'Content-Type': 'application/json' },
                      body: JSON.stringify({ image: imageBase64 })
                    });
                    await showJson(res);
                  } catch (err) {
                    setStatus('Request error', false);
                    outEl.textContent = String(err);
                  } finally {
                    runLivenessBtn.disabled = false;
                  }
                });
              </script>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }

    [HttpGet("/api/dev/dojah/bvn")]
    public async Task<IActionResult> LookupBvn(
        [FromQuery] string? bvn,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        if (!EnsureSandbox())
        {
            return SandboxRequired();
        }

        var targetBvn = string.IsNullOrWhiteSpace(bvn) ? SandboxTestBvn : bvn.Trim();
        var result = await dojahClient.LookupBvnAsync(targetBvn, cancellationToken);
        return ToProbeResult(
            result.IsSuccess,
            result.StatusCode,
            result.ErrorMessage,
            result.RawBody,
            new
            {
                bvn = targetBvn,
                firstName = result.FirstName,
                lastName = result.LastName,
                dateOfBirth = result.DateOfBirth,
            });
    }

    [HttpPost("/api/dev/dojah/liveness")]
    [RequestSizeLimit(8_000_000)]
    public async Task<IActionResult> CheckLiveness(
        [FromBody] LivenessProbeRequest? request,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        if (!EnsureSandbox())
        {
            return SandboxRequired();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Image))
        {
            return BadRequest(new { message = "Request body must include { \"image\": \"<base64 or data-url>\" }." });
        }

        var result = await dojahClient.CheckLivenessAsync(request.Image, cancellationToken);
        return ToProbeResult(
            result.IsSuccess,
            result.StatusCode,
            result.ErrorMessage,
            result.RawBody,
            new { check = "liveness" });
    }

    private bool EnsureSandbox() => IsSandboxBaseUrl(dojahOptions.Value.BaseUrl);

    private BadRequestObjectResult SandboxRequired() => BadRequest(new
    {
        message = "Refusing to call non-sandbox Dojah BaseUrl from the unauthenticated probe.",
        baseUrl = dojahOptions.Value.BaseUrl,
    });

    private IActionResult ToProbeResult(
        bool isSuccess,
        int statusCode,
        string? errorMessage,
        string? rawBody,
        object extra)
    {
        object? body = null;
        if (!string.IsNullOrWhiteSpace(rawBody))
        {
            try
            {
                body = System.Text.Json.JsonSerializer.Deserialize<object>(rawBody);
            }
            catch
            {
                body = rawBody;
            }
        }

        var payload = new Dictionary<string, object?>
        {
            ["success"] = isSuccess,
            ["statusCode"] = statusCode,
            ["error"] = errorMessage,
            ["dojah"] = body,
        };

        foreach (var prop in extra.GetType().GetProperties())
        {
            payload[prop.Name] = prop.GetValue(extra);
        }

        if (!isSuccess)
        {
            return StatusCode(statusCode > 0 ? statusCode : StatusCodes.Status502BadGateway, payload);
        }

        return Ok(payload);
    }

    private static bool IsSandboxBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return false;
        }

        return baseUrl.Contains("sandbox.dojah.io", StringComparison.OrdinalIgnoreCase);
    }
}
