# HTTP endpoints

Any mod built on Ducky.Sdk can declare HTTP endpoints under its own id with `Contract.Http`. Exactly one mod at a time
serves them to the outside world by holding the host role with `Contract.HttpHost`. The SDK itself opens no port. The
host mod owns the listener, its address, its authentication and its transport.

The channel runs on the existing mod message hub (`ModHttpV1`). Nothing is sent to the hub until a host is present.

## Declaring endpoints (any mod)

```csharp
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Contracts.HttpEndpoints;

// Synchronous handler: answers at once.
var status = Contract.Http.Map("GET", "/Acme.Dash/status", request =>
    HttpEndpointResponse.Json(200, "{\"ok\":true}"));

// Asynchronous handler: may await. It runs on the Unity main thread, so Unity APIs are allowed.
var frame = Contract.Http.Map("GET", "/Acme.Dash/frame", async request =>
{
    await UniTask.Yield();
    return HttpEndpointResponse.Text(200, Time.frameCount.ToString());
});

// Withdraw an endpoint by disposing its handle.
status.Dispose();
```

- `Map(method, path, handler)` returns an `IDisposable`. Dispose it to remove the endpoint once; a second dispose does nothing.
- The path must start with the mod id the SDK reports, which the example `/Acme.Dash` only stands for. For a mod in a local
  folder that id is `local.<folder>`, so a mod named `Ducky.HttpEndpointSample` maps under `/local.Ducky.HttpEndpointSample/`.
  Build the prefix from the SDK's id rather than hard-coding it: `"/" + Helper.GetModId().Value` (the SDK compiles
  `Helper` into each mod). `Samples/Ducky.HttpEndpointSample` does this.
- `Map` never throws because no host exists. It records the endpoint locally, sends nothing and logs nothing per call.
- `Map` throws an `ArgumentException` for an unsupported method, a path outside `/<modId>`, a bad segment or a duplicate
  method and path. The first endpoint stays in place when a duplicate is refused.
- `Contract.Http.IsHostAvailable` is true while an `httphost` is registered on the hub.

## Hosting (the host mod only)

```csharp
using System;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Contracts.HttpEndpoints;

// ModEnabled(): claim the role. Returns false, with one warning, if another mod already holds it.
bool isHost = Contract.HttpHost.Start();

// A listener (owned by the host mod, bound to loopback or another address you choose) calls ForwardAsync once per request.
async UniTask HandleExternal(HttpEndpointRequest request)
{
    HttpEndpointResponse response = await Contract.HttpHost.ForwardAsync(request);
    // write response.Status, response.Headers, response.ContentType and response.Body to the external client
}

// Routes can be listed, and a change raises RoutesChanged on the main thread.
var routes = Contract.HttpHost.Routes;
Contract.HttpHost.RoutesChanged += () => { /* refresh a listing */ };

// ModDisabled(): release the role. In-flight requests get 502, the routes are cleared and httphost is unregistered.
Contract.HttpHost.Stop();
```

- `ForwardAsync` never throws and may be called from any thread, including a thread-pool thread that an `HttpListener`
  uses. It returns a response in every case.
- `Stop()` is safe to call when the role is not held. `Contract.HttpHost.IsHost` tells whether this mod holds it.

## Hub ids

| Id | Who | Purpose |
|----|-----|---------|
| `httphost` | the host mod | the single host, registered on the hub |
| `<modId>.http` | each mod with endpoints | the mod's hub client; also the sender identity |

The host reads the sender's mod id from its hub client id by removing the `.http` suffix. The mod's own id stays free for its
other hub handlers.

## Messages

Six content types. Bodies are JSON objects. Readers ignore unknown fields.

| Content type | From | To | Purpose |
|--------------|------|----|---------|
| `ducky.http.hello` | client | host | lists the versions the client speaks |
| `ducky.http.ready` | host | client | names the chosen version, `0` for none |
| `ducky.http.register.v1` | client | host | the full snapshot of the client's endpoints |
| `ducky.http.registered.v1` | host | client | accepted and rejected endpoints with reasons |
| `ducky.http.request.v1` | host | client | one external request to forward |
| `ducky.http.response.v1` | client | host | the answer, with the request's `id` |

Example bodies (these are the exact texts the protocol tests decode):

```json
hello:            {"client":"Acme.Dash","versions":[1]}
ready:            {"version":1,"supported":[1]}
register.v1:      {"mod":"Acme.Dash","endpoints":[{"method":"GET","path":"/Acme.Dash/status"}]}
registered.v1:    {"accepted":[{"method":"GET","path":"/Acme.Dash/status"}],"rejected":[{"method":"GET","path":"/other/x","reason":"the path must start with '/Acme.Dash'"}]}
request.v1:       {"id":"6f1c2a","method":"GET","path":"/Acme.Dash/find","query":[["tag","a"],["tag","b"]],"headers":{"accept":"text/plain"},"contentType":"","body":""}
response.v1:      {"id":"6f1c2a","status":200,"headers":{"content-type":"text/plain; charset=utf-8"},"contentType":"text/plain; charset=utf-8","body":"ok"}
```

- `request.v1` carries `query` as an ordered list of `[name, value]` pairs, so a repeated name keeps its order.
- `register.v1` replaces every endpoint of the sender. An empty `endpoints` array withdraws them all.
- A `registered.v1` entry with a reason says why the host refused it.

## Versions

- The version is part of the content type for `register.v1`, `registered.v1`, `request.v1` and `response.v1`. A later
  incompatible format becomes `...v2` and is sent only after negotiation.
- `hello` and `ready` have no version suffix and never change. They only gain fields, so any two SDK builds can shake hands.
- The client sends `hello` with the versions it supports. The host answers `ready` with the highest common version, or `0`.
  A `0` makes the client log one warning and stay silent until the host leaves and returns.
- Within one version, fields are only added. Readers ignore unknown fields. An endpoint entry without a valid `method` or
  `path` is skipped without discarding the others.

## Path and method rules

- Methods: `GET`, `POST`, `PUT`, `PATCH` or `DELETE`, case-insensitive.
- A path is `/<modId>` or `/<modId>/` followed by segments. A trailing slash is ignored.
- `<modId>` uses only letters, digits, `.`, `_` and `-`. The path uses only letters, digits, `.`, `_`, `-`, `~` and `/`.
- No query string, fragment, empty segment, `.` or `..` segment.
- Matching is exact and case-sensitive. There are no wildcards or parameters.

Accepted: `/Acme.Dash/api/stats`, `/Acme.Dash`, `/Acme.Dash/`.
Refused: `/other/x`, `/Acme.Dash/../x`, `/Acme.Dash/a?b=1`, `/Acme.Dash//x`.

## Limits and status codes

| Limit | Value | Enforced by |
|-------|-------|-------------|
| Request and response body | 65536 bytes of UTF-8 | host (`413` for a request), client (`502` for a response) |
| Round trip | 15 seconds | host (`504`) |
| Requests in flight | 64 | host (`503`) |
| Endpoints per mod | 64 | host (extra endpoints are rejected in `registered.v1`) |
| Host presence check | at most every 5 seconds, only while the table is not empty | client and host |

| Status | Meaning |
|--------|---------|
| `404` | no endpoint serves the method and path |
| `413` | the request body is too large; nothing was sent over the hub |
| `502` | the handler threw, the answer was invalid or oversized, or the owner left the hub |
| `503` | the host role is not held, or 64 requests are in flight |
| `504` | the endpoint did not answer within 15 seconds |

A thrown handler exception is logged once by the mod. Its text is never sent to the external client.

## Latency and handler length

A round trip goes through the hub, which wakes about every 100 ms, so expect a few hundred milliseconds per request. The
endpoints suit dashboards, scripts and companion tools, not per-frame use. Keep handlers short. A handler that blocks the
main thread stalls the game, so use the asynchronous overload for waits. The 15 second timeout bounds only the HTTP side.

## Security

- The SDK opens no port and reads no network input.
- The hub does not authenticate the sender of a message, so the path rule stops mistakes between cooperating mods, not a
  hostile mod.
- Access control, TLS and any address binding belong to the host mod. Bind a loopback address unless you have decided
  otherwise.

## Host mod outline

1. `ModEnabled()`: call `Contract.HttpHost.Start()` and, if it returns false, log that another host holds the role.
2. Start a listener on the address you chose. For each request, call `Contract.HttpHost.ForwardAsync(request)` and write
   the response back.
3. `ModDisabled()`: stop the listener, then call `Contract.HttpHost.Stop()`.

The `Samples/Ducky.HttpHostSample` project shows this outline with a loopback-only `HttpListener`. It is a test aid, not a
product.

## Lifecycle

When a mod is disabled, its endpoints are withdrawn: an empty snapshot is sent to an announced host and its hub client is
unregistered. This happens in every build, through the shared teardown of `ModBehaviourBase`. A host that is disabled
releases `httphost`, and every enabled endpoint mod announces again to the next host it finds.

### Hub replaced: endpoint mods announce again

When the hub object itself is replaced (the mod that started it was hot reloaded, so the old hub is destroyed and a new one
starts empty), each endpoint mod notices the new hub object on its next tick. Its hub client registers on the new hub once,
the mod negotiates with the host again and announces the whole table in one snapshot. This needs no reload of the endpoint
mods and no game restart. Requests that arrive before the host has seen the new snapshot get `404`.

### Known limitation: a host that re-registers on an unchanged hub within one check interval

Endpoint mods learn that a host went away only at their presence check, which runs at most every 5 seconds. A host that
unregisters and registers again on the same hub object inside one check interval is not noticed. The endpoint mods keep
their state, the host has no routes, and the endpoints answer `404` until the endpoint mods are reloaded. A host that is
seen absent at a check is handled: the endpoint mods negotiate and announce to it again. The SDK has no signal from the hub
for this case, so the check is the only detection.

### Known risk: a late teardown of an old Core after its replacement announced

When an old Core is released after the new Core has already announced its endpoints, the old client still sends its
empty withdrawal (it was announced when it was released). The host then drops the routes of that mod, including the ones
the new Core announced, until the new Core announces again. Its hub registration is not affected, so the new Core keeps
receiving messages. The normal reload order (old Core released before the new one enables) does not trigger this. The
SDK does not yet tell an owned withdrawal from a superseded one; the fix would be for the transport's unregister to report
whether it removed the caller's own registration. The scenario test `LateTeardownOfTheOldCoreKeepsTheNewRegistrationServing`
covers the case where the new Core has registered but not yet announced.

### Hot reload of the samples

The client sample (`Samples/Ducky.HttpEndpointSample`) is reloaded; its `/status` answer names the Core assembly, so a
caller sees which Core is live. The host sample (`Samples/Ducky.HttpHostSample`) is not reloaded. It logs the route count
on every change, and its loopback-only listing at `/httphost/routes` shows the route table, so a check can confirm that
each endpoint is listed once. The HTTP host mod is a consumer-side mod: its hot reload support is outside what the SDK
controls, and the SDK does not try to make it survive a reload. The loop-back scenario tests in
`Sdk/Tests/Ducky.Sdk.Lib.Tests/HotReloadScenarioTests.cs` cover the client reload, the hub replacement and a late teardown
of the old Core.
