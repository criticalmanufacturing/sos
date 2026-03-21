# CMF CLI SoS Plugin v1.0.0

![soscontainerimage](/images/SOSContainer.png)

## Introduction

The **SoS Plugin** is a command-line diagnostic utility designed to orchestrate advanced troubleshooting from Kubernetes pods. It automates the complex process of attaching debuggers, capturing memory dumps, and collecting performance metrics for **.NET** and **Node.js** applications without disrupting running workloads and without introducing security risks.

## Core Features

* **Automated Memory Dumps (`dump`)**
  * Trigger and download memory/heap dumps directly to your local machine.
  * Automatically detects whether the target pod is running .NET or Node.js.
  * **.NET Support:** Uses `dotnet-dump collect` to capture complete core dumps (`.dmp`).
  * **Node.js Support:** Captures V8 heap snapshots (`.heapdump`) via the Inspector Protocol by injecting an extraction script.

* **Runtime Metrics (`runtimeMetrics`)**
  * Collect runtime performance counters from .NET pods for a specific duration using `dotnet-counters`.
  * Supports customizable payloads (e.g., `System.Runtime`) and various output formats (JSON, CSV).

* **Node.js Remote Debugging (`Remote Debug`)**
  * Seamlessly attach a remote debugger to a running Node.js pod.
  * Automatically sends the `USR1` signal to enable the V8 inspector without restarting the application.
  * Establishes a secure local port-forwarding session to connect directly via Chrome/Edge DevTools (`chrome://inspect`).

* **Advanced Kubernetes Orchestration**
  * **Zero-Impact Debugging:** Leverages `kubectl debug` to attach ephemeral debugger containers using shared process namespaces, ensuring the target application is never restarted or mutated.
  * **Auto-PID Discovery:** Automatically resolves the target container and Process ID (PID) if not explicitly provided by the user.
  * **Artifact Retrieval:** Handles staging the output files inside the cluster and safely streaming them back to the user's local filesystem (`kubectl cp`).
  * **Automatic Cleanup:** Guarantees that debugging sessions and ephemeral containers are gracefully terminated and cleaned up once the extraction is complete.

## Requirements

* `kubectl` configured with active access to the target Kubernetes cluster.