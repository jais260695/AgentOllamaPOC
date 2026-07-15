# AI Agent Framework for .NET

A modular AI Agent Framework built with **.NET**, **Microsoft.Extensions.AI**, **Gemini/Ollama**, **Redis**, **PostgreSQL**, **Qdrant**, and **Model Context Protocol (MCP)**.

The goal of this project is to demonstrate how to build production-ready AI agents with:

* Multi-agent architecture
* Long-term semantic memory
* Conversation summarization
* RAG (Retrieval Augmented Generation)
* Structured outputs
* Streaming responses
* Tool calling via MCP
* Provider-independent LLM execution

---

# Features

## AI Agents

* Router Agent
* GitHub Agent
* RAG Agent
* Conversation Summary Agent
* Memory Extraction Agent

---

## Memory System

### Short-Term Memory

* Redis
* Sliding window conversation history
* Recent message retrieval

### Conversation Memory

* PostgreSQL
* Automatic summarization
* Configurable summarization threshold

### Long-Term Semantic Memory

* Qdrant Vector Database
* Embedding-based retrieval
* Duplicate detection
* Importance scoring
* User-specific memories

---

## Retrieval Augmented Generation (RAG)

* GitHub repository indexing
* Embeddings using nomic-embed-text
* Vector search using Qdrant
* Context injection into prompts

---

## Agent Framework

* Generic AgentExecutor<T>
* Prompt Builder
* Structured JSON Outputs
* Streaming Responses
* Retry Support
* Provider Independent

---

## Tool Calling

Supports Model Context Protocol (MCP)

Example:

* GitHub repositories
* Issues
* Pull Requests
* Commits
* Branches

---

# Architecture

```
                     User
                       │
                       ▼
                AgentService
                       │
                RouterAgent
             ┌─────────┴─────────┐
             ▼                   ▼
       GithubAgent          RagAgent
             │                   │
             └─────────┬─────────┘
                       ▼
                 AgentExecutor<T>
                       │
                 PromptBuilder
        ┌──────────┼─────────────┐
        ▼          ▼             ▼
 Recent Redis   Summary DB   Semantic Memory
        │          │             │
        └──────────┴──────┬──────┘
                           ▼
                     ChatClient
                  Gemini / Ollama
```

---

# Memory Flow

```
Conversation

↓

Redis

↓

Conversation Summary

↓

Memory Extraction

↓

Semantic Memory

↓

Qdrant

↓

Semantic Search

↓

Prompt Builder

↓

LLM
```

---

# Technology Stack

## Backend

* .NET 10
* C#
* Microsoft.Extensions.AI

## AI Models

* Gemini
* Ollama

## Embeddings

* nomic-embed-text

## Memory

* Redis
* PostgreSQL
* Qdrant

## Tool Calling

* Model Context Protocol (MCP)

## Logging

* Microsoft.Extensions.Logging

---

# Project Structure

```
src/

├── Agents/
│   ├── RouterAgent
│   ├── GithubAgent
│   └── RagAgent
│
├── Execution/
│   ├── AgentExecutor
│   ├── ExecutionResult
│   └── PromptBuilder
│
├── Memory/
│   ├── Redis
│   ├── PostgreSQL
│   ├── SemanticMemory
│   └── MemoryExtraction
│
├── Rag/
│
├── Prompts/
│
├── Models/
│
└── Tools/
```

---

# AI Execution Pipeline

```
User

↓

AgentService

↓

Router Agent

↓

AgentExecutor<T>

↓

Prompt Builder

↓

Chat Client

↓

LLM

↓

Structured Output

↓

Streaming Response
```

---

# Semantic Memory

The framework automatically extracts long-term memories from conversations.

Example:

Conversation:

```
User:
I prefer PostgreSQL over MongoDB.

User:
I'm building an AI framework using .NET.
```

Extracted memories:

```
User prefers PostgreSQL.

User is building an AI framework using .NET.
```

These memories are:

* Embedded
* Stored in Qdrant
* Retrieved using semantic similarity
* Added back into future prompts

---

# Conversation Summary

Older messages are automatically summarized once the conversation reaches a configurable threshold.

Prompt Builder combines:

* Relevant semantic memories
* Conversation summary
* Recent messages

This minimizes token usage while preserving context.

---

# Structured Outputs

Agents return strongly typed responses instead of plain text.

Example:

```csharp
ExecutionResult<RouteDecision>

ExecutionResult<SummaryResponse>

ExecutionResult<MemoryExtractionResponse>

ExecutionResult<string>
```

---

# Streaming

Supports token streaming using `IAsyncEnumerable<ChatResponseUpdate>`.

---

# Current Capabilities

* Multi-Agent Routing
* Semantic Memory
* Conversation Summaries
* Redis Short-Term Memory
* PostgreSQL Summaries
* Qdrant Vector Search
* GitHub RAG
* MCP Tool Calling
* Structured Outputs
* Streaming
* Generic Agent Executor

---

# Future Roadmap

## Memory

* Memory decay
* Memory consolidation
* Memory update
* Memory deletion
* User profile memory

## Planning

* Planner Agent
* Reflection Agent
* Critic Agent
* Multi-step execution

## Multi-Agent

* Planner
* Research Agent
* Coding Agent
* Reviewer Agent

## Observability

* OpenTelemetry
* Prometheus
* Grafana
* Distributed Tracing
* AI Cost Tracking

## Reliability

* Retry policies
* Circuit breakers
* Rate limiting
* Model fallback
* Provider routing

---

# Goals

This project aims to demonstrate production-ready AI engineering patterns using modern .NET technologies while remaining provider independent and easily extensible.
