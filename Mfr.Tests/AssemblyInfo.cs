// CliArgParser uses a static capture during Spectre parsing; sequential tests avoid concurrent ParseArgs calls.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
