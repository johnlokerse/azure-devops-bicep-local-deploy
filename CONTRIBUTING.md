# Contributing to Bicep Azure DevOps extension

<!-- markdownlint-disable MD007 -->
- [Contributing to Bicep Azure DevOps extension](#contributing-to-bicep-azure-devops-extension)
  - [Contribution Workflow](#contribution-workflow)
  - [Adding a new resource](#adding-a-new-resource)
  - [Code conventions](#code-conventions)

Every contribution is welcome to the Bicep Azure DevOps extension.
We make use of GitHub issues to track reported issues by the community.
GitHub pull request are used to merge in code changes.

## Contribution Workflow

Code contributions follow a GitHub-centered workflow. To participate in
the development of the Bicep Azure DevOps extension, you require a GitHub account first.

Then, you can follow the steps below:

1. Fork this repo by going to the project repo page and use the _Fork` button.
2. Clone down the repo to your local system

    ```bash
    git clone https://github.com/<username>/azure-devops-bicep-local-deploy.git
    ```

3. Create a new branch to hold your code changes you want to make:

    ```bash
    git checkout -b branch-name
    ```

4. Work on your code and test it if applicable.

When you are done with your work, make sure you commit the changes to
your branch. Then, you can open a pull request on this repository.

## Adding a new resource

To add a new resource:

1. Create a new directory or add a new handler in the `Handlers` directory
2. Implement the model in the `Models` directory
3. Test your changes locally by running `bicep local-deploy` or use `grpcurl`
4. Open a PR for review

> [!NOTE]
> The directory structure follows the REST API reference on the official [Azure DevOps
> REST API][00] documentation.

## Code conventions

<!-- TODO: Write about conventions -->

<!-- Link reference definitions -->
[00]: https://learn.microsoft.com/en-us/rest/api/azure/devops/?view=azure-devops-rest-7.2
