using KatzuoOgust.Cqrs.Examples;
using KatzuoOgust.Cqrs.Examples.Behaviours;
using KatzuoOgust.Cqrs.Examples.Decorators;
using KatzuoOgust.Cqrs.Examples.Flow;
using KatzuoOgust.Cqrs.Examples.Middlewares;

await DecoratorsExample.RunAsync();
await MiddlewaresExample.RunAsync();
await BehavioursExample.RunAsync();
await FlowExample.RunAsync();
