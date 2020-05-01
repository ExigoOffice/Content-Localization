// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Not applicable to test projects", Scope = "module")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Not applicable to test projects", Scope = "module")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Not applicable to test projects", Scope = "module")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Not applicable to test projects", Scope = "module")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Yeah I don't agree with this", Scope = "module")]
