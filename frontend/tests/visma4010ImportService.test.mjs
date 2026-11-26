import assert from "node:assert/strict";
import { test } from "node:test";
import { importVisma4010 } from "../src/Services/visma4010ImportService.ts";

test("shows the import failure detail returned by the API", async (t) => {
    const detail = "Förhandsgranska filen igen innan du försöker importera på nytt.";
    t.mock.method(globalThis, "fetch", async () => Response.json({ detail }, { status: 500 }));

    const result = await importVisma4010(new File(["test"], "test.xlsx"));

    assert.deepEqual(result, { kind: "error", message: detail });
});

test("shows a useful fallback when a server failure returns HTML", async (t) => {
    t.mock.method(globalThis, "fetch", async () => new Response("<html>Error</html>", { status: 502 }));

    const result = await importVisma4010(new File(["test"], "test.xlsx"));

    assert.equal(result.kind, "error");
    assert.match(result.message, /Förhandsgranska filen igen/);
    assert.doesNotMatch(result.message, /<html>/);
});

for (const detail of [{ database: "internal" }, "", "   ", 42]) {
    test(`uses the fallback for an invalid problem detail: ${JSON.stringify(detail)}`, async (t) => {
        t.mock.method(globalThis, "fetch", async () => Response.json({ detail }, { status: 500 }));
        const result = await importVisma4010(new File(["test"], "test.xlsx"));
        assert.equal(result.kind, "error");
        assert.equal(typeof result.message, "string");
        assert.match(result.message, /Förhandsgranska filen igen/);
    });
}

for (const status of [201, 409, 422]) {
    test(`returns a recoverable error when the ${status} response body is interrupted`, async (t) => {
        t.mock.method(globalThis, "fetch", async () => new Response('{"fileName":', { status }));
        const result = await importVisma4010(new File(["test"], "test.xlsx"));
        assert.equal(result.kind, "error");
        assert.match(result.message, /Förhandsgranska filen igen/);
    });
}

test("asks for a preview if the connection is lost before the result is known", async (t) => {
    t.mock.method(globalThis, "fetch", async () => { throw new TypeError("Failed to fetch"); });
    const result = await importVisma4010(new File(["test"], "test.xlsx"));
    assert.equal(result.kind, "error");
    assert.match(result.message, /Förhandsgranska filen igen/);
});

for (const status of [201, 409, 422]) {
    test(`preserves a complete ${status} import result`, async (t) => {
        const data = { fileName: "test.xlsx", canImport: status === 201, blockers: [], toImport: { count: 2, netSek: 300 } };
        t.mock.method(globalThis, "fetch", async () => Response.json(data, { status }));
        const result = await importVisma4010(new File(["test"], "test.xlsx"));
        assert.deepEqual(result, { kind: status === 201 ? "ok" : "blocked", data });
    });
}
