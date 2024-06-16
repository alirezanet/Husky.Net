To debug the tests remotely:

1. Run the test in debug mode.
2. The test will freeze when Husky processes run.
3. In Rider, go to "Attach to Remote Process."
4. Select Docker and locate the test container.
5. Install remote debugging tools and select the running .NET process.
6. It should hit the breakpoint in the code
