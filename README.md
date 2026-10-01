# CMF SoS Plugin v1.1.0

![soscontainerimage](/images/SOSContainer.png)

## Introduction

The **SoS Plugin** is a command-line diagnostic utility designed to orchestrate advanced troubleshooting from Kubernetes pods. It automates the complex process of attaching debuggers, capturing memory dumps, and collecting performance metrics for **.NET** and **Node.js** applications without disrupting running workloads and without introducing security risks.

## Core Features

* **Automated Memory Dumps (`dump`)**
  * Trigger and download memory/heap dumps directly to your local machine.
  * Automatically detects whether the target pod is running .NET or Node.js.

* **Runtime Metrics (`runtimeMetrics`)**
  * Collect runtime performance counters from .NET pods for a specific duration using `dotnet-counters`.
  * Supports customizable payloads (e.g., `System.Runtime`) and various output formats (JSON, CSV).

* **Node.js Remote Debugging (`Remote Debug`)**
  * Attach a remote debugger to a running Node.js pod.
  * Automatically sends the `USR1` signal to enable the V8 inspector without restarting the application.
  * Establishes a secure local port-forwarding session to connect directly via Chrome/Edge DevTools (`chrome://inspect`).

* **Interactive Shell**
  * Attach a remote debugger to a running pod and have full access to the debugger console.

* **Advanced Kubernetes Orchestration**
  * **Zero-Impact Debugging:** Leverages `kubectl debug` to attach ephemeral debugger containers using shared process namespaces, ensuring the target application is never restarted or mutated.
  * **Auto-PID Discovery:** Automatically resolves the target container and Process ID (PID) if not explicitly provided by the user.
  * **Artifact Retrieval:** Handles staging the output files inside the cluster and safely streaming them back to the user's local filesystem (`kubectl cp`).
  * **Automatic Cleanup:** Guarantees that debugging sessions and ephemeral containers are terminated and cleaned up once the extraction is complete.

## Requirements

* `kubectl` configured with active access to the target Kubernetes cluster.

## Usage

- Execute with no arguments to have access to the interactive UI.
- Execute with arguments to use the system commandline with fully customizable commands.

## Configurable URLs and images

Set these environment variables before running SoS to use your own registries,
debug images, or symbol server. They apply to both the command line and interactive
UI. Unset, empty, or whitespace-only values use the defaults below; surrounding
whitespace is trimmed from configured values.

| Environment variable | Used for | Default value |
| --- | --- | --- |
| `cmf_sos_registry` | npm registry where SoS image should be | `https://dev.criticalmanufacturing.io/repository/npm-public` |
| `cmf_sos_debug_image` | Dumps, runtime metrics, interactive shells, and Node.js remote debugging image | `dev.criticalmanufacturing.io/platformengineering/sos:latest` |
| `cmf_sos_ubi_debug_image` | .NET remote debugging (the UBI-based debug container) image | `dev.criticalmanufacturing.io/platformengineering/sos-ubi:latest` |
| `cmf_sos_symbol_server` | Base URL for .NET debugging symbols where SoS appends `/<appVersion>` | `https://symbolserver.apps.rhos.cm-mes.dev` |

Registry and symbol-server settings are HTTP(S) URLs. Debug-image settings are
container image references (`registry/repository:tag`), without an `https://` prefix.

For example:

```bash
export cmf_sos_registry=https://npm.example.com
export cmf_sos_debug_image=registry.example.com/tools/sos:latest
export cmf_sos_ubi_debug_image=registry.example.com/tools/sos-ubi:latest
export cmf_sos_symbol_server=https://symbols.example.com
cmf-sos
```

An explicit `--image` command-line option overrides the relevant debug-image
environment variable for that command. It does not change the npm registry or
symbol server.

### Base image when building the debug container

The Docker build argument `CMF_SOS_BASE_IMAGE` sets the base image for
`src/sidecar/Dockerfile`. Its default is
`dev.criticalmanufacturing.io/platformengineering/ubuntu-base-24.04:latest`.
This is a build-time setting, separate from the runtime environment variables above.
Use a compatible base image that supplies the dependencies and user variables
expected by the Dockerfile.

From the repository root:

```bash
docker build \
  --build-arg CMF_SOS_BASE_IMAGE=registry.example.com/tools/ubuntu-base-24.04:latest \
  -t registry.example.com/tools/sos:latest \
  src/sidecar
```

## Development

Open the repository in the VS Code devcontainer to get the development tools and
Codex extension (`openai.chatgpt`). Rebuild an existing container to install the
extension, then sign in using the [official Codex IDE setup](https://learn.chatgpt.com/docs/codex/ide).
