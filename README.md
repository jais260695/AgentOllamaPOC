# AgentOllamaPOC

## Overview

AgentOllamaPOC is a .NET AI Agent application built using Ollama, Microsoft Agents AI, Model Context Protocol (MCP), and Retrieval Augmented Generation (RAG).

The application demonstrates:

- Local LLM execution using Ollama
- GitHub automation using MCP tools
- Repository understanding using RAG
- Vector search using Qdrant
- AI agent based tool calling

---

# Architecture

The application follows this architecture:

                User
                  |
                  v
          OllamaAgentService
                  |
      +-----------+-----------+
      |                       |
      v                       v
GithubAgent                RagService
      |                       |
      v                       v
GitHub MCP              Qdrant Vector DB
      |
      v
 GitHub API

---

# Components

## OllamaAgentService

Responsible for application orchestration.

Responsibilities:

- Creates Ollama chat client
- Connects GitHub MCP client
- Initializes RAG services
- Sends user questions to the AI agent


---

## GithubAgent

The AI agent responsible for GitHub operations.

Capabilities:

- Uses MCP tools
- Searches repositories
- Reads files
- Retrieves commits
- Works with GitHub issues and pull requests

The agent does not guess repository information.

It always uses available GitHub tools.

---

## GitHub MCP Client

The application connects with GitHub using Model Context Protocol.

The MCP server provides tools such as:


search_repositories
get_file_contents
search_code
list_commits
create_issue
create_pull_request


Authentication is handled using:


GITHUB_PERSONAL_ACCESS_TOKEN


environment variable.

---

# RAG Implementation

AgentOllamaPOC uses Retrieval Augmented Generation.

Pipeline:


Document
|
v
Text Chunking
|
v
Ollama Embedding Model
|
v
Vector Generation
|
v
Qdrant Vector Database
|
v
Similarity Search
|
v
Ollama LLM Response


---

# Embedding Model

The application uses Ollama embedding model:


nomic-embed-text


The model converts text into vector embeddings.

Vector size:


768 dimensions


---

# Vector Database

Qdrant is used for storing embeddings.

Collection:


github_code


Each stored document contains:


FileName
Content
Embedding Vector


---

# Example Indexed Document

Example:


README.md

Content:
Authentication uses GitHub Personal Access Token.

The MCP client connects with GitHub APIs.


The content is converted into embeddings and stored in Qdrant.

---

# Example Questions

## RAG Questions

Ask:


Explain authentication flow


Expected:

The answer should come from README/document context.

---

Ask:


Explain the RAG pipeline


Expected:

The AI explains:

- Document chunking
- Embedding creation
- Qdrant storage
- Similarity search
- Final LLM response


---

## GitHub MCP Questions

Ask:


search repositories for user user:jais260695 using github tool and english language


The agent uses:


search_repositories


tool.

---

Ask:


Fetch commits of CodeReviewAgent repository owned by jais260695 in main branch


The agent uses:


list_commits


tool.

---

# Technologies

- .NET
- Microsoft.Agents.AI
- Microsoft.Extensions.AI
- Ollama
- Qwen2.5
- nomic-embed-text
- Model Context Protocol
- GitHub MCP Server
- Qdrant

---

# Future Enhancements

Possible improvements:

- Index complete GitHub repositories automatically
- Store repository files in Qdrant
- Add code-level semantic search
- Add multi-agent architecture
- Add persistent chat memory
- Add GitHub repository analyzer agent