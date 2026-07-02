using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 7216;
});

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", Home);
app.MapPost("/Predict", Predict);

app.Run();

static IResult Home()
{
    const string html = """
        <!doctype html>
        <html lang="zh-Hant">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Predict</title>
            <style>
                :root {
                    color-scheme: light;
                    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                    background: #f4f6f8;
                    color: #1f2933;
                }

                * {
                    box-sizing: border-box;
                }

                body {
                    margin: 0;
                    min-height: 100vh;
                    display: grid;
                    place-items: center;
                    padding: 32px;
                }

                main {
                    width: min(720px, 100%);
                    display: grid;
                    gap: 16px;
                }

                h1 {
                    margin: 0;
                    font-size: 28px;
                    font-weight: 700;
                }

                label {
                    display: grid;
                    gap: 8px;
                    font-weight: 600;
                }

                textarea {
                    width: 100%;
                    min-height: 160px;
                    resize: vertical;
                    border: 1px solid #c8d0d9;
                    border-radius: 8px;
                    padding: 14px;
                    font: 15px ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
                    line-height: 1.5;
                    background: #ffffff;
                    color: #111827;
                }

                textarea:focus {
                    outline: 3px solid #b8dafc;
                    border-color: #3b82f6;
                }

                button {
                    justify-self: start;
                    border: 0;
                    border-radius: 8px;
                    padding: 10px 18px;
                    background: #2563eb;
                    color: #ffffff;
                    font-size: 16px;
                    font-weight: 700;
                    cursor: pointer;
                }

                button:disabled {
                    cursor: wait;
                    opacity: 0.72;
                }

                #result {
                    background: #eef2f7;
                }
            </style>
        </head>
        <body>
            <main>
                <h1>Predict</h1>
                <label for="payload">
                    輸入
                    <textarea id="payload" spellcheck="false">{"value":42,"text":"hello"}</textarea>
                </label>
                <button id="predictButton" type="button">預測</button>
                <label for="result">
                    輸出結果
                    <textarea id="result" readonly></textarea>
                </label>
            </main>
            <script>
                const payload = document.querySelector("#payload");
                const result = document.querySelector("#result");
                const predictButton = document.querySelector("#predictButton");

                predictButton.addEventListener("click", async () => {
                    predictButton.disabled = true;
                    result.value = "";

                    try {
                        const parsedPayload = JSON.parse(payload.value);
                        const response = await fetch("/Predict", {
                            method: "POST",
                            headers: { "Content-Type": "application/json" },
                            body: JSON.stringify(parsedPayload)
                        });
                        const responseText = await response.text();
                        const responseBody = responseText ? JSON.parse(responseText) : null;
                        result.value = JSON.stringify(responseBody, null, 2);
                    } catch (error) {
                        result.value = error instanceof Error ? error.message : String(error);
                    } finally {
                        predictButton.disabled = false;
                    }
                });
            </script>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html; charset=utf-8");
}

static IResult Predict(JsonElement request)
{
    if (request.ValueKind != JsonValueKind.Object)
    {
        return Results.BadRequest(new { Message = "Request body must be a JSON object." });
    }

    return Results.Ok(new
    {
        Message = "Prediction request received.",
        Input = request,
    });
}
