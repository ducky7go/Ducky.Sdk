# ModOptions Configuration Management Specification

This specification defines the behavior and requirements for the ModOptions configuration management system, which provides save/load functionality for mod configurations with support for various data types.

## Overview

ModOptions is a configuration management system that handles:
- Primitive type storage (int, string, bool, etc.)
- DateTime/DateTimeOffset serialization using Unix timestamps
- Complex type serialization using JSON
- Collection type serialization (lists, arrays, dictionaries)
- Multiple storage backends (ES3 file-based and in-memory)

## Architecture

The system uses a type classification approach where:
- **Simple types** are stored directly in their native format
- **Complex types** are serialized to JSON strings before storage
- **DateTime types** are converted to Unix timestamps for efficient storage

## Core Requirements

### Requirement: Simple Type Classification
The `IsSimpleType()` method SHALL correctly classify data types to determine appropriate storage mechanisms.

#### Scenario: Simple Type Detection
- **WHEN** `IsSimpleType()` is called with primitive types, enums, string, decimal, DateTime, etc.
- **THEN** it SHALL return `true` to indicate direct storage

#### Scenario: Simple Type Detection (Unchanged)
- **WHEN** `IsSimpleType()` is called with primitive types, enums, string, decimal, DateTime, etc.
- **THEN** it SHALL return `true` as before

### Requirement: DateTime Type Handling
ModOptions SHALL handle DateTime and DateTimeOffset types using Unix timestamp conversion.

#### Scenario: DateTime Serialization
- **WHEN** saving DateTime or DateTimeOffset types
- **THEN** the values SHALL be converted to Unix timestamps
- **AND** SHALL be stored as long integers

#### Scenario: DateTime Deserialization
- **WHEN** loading DateTime or DateTimeOffset types
- **THEN** the Unix timestamps SHALL be converted back to their original types