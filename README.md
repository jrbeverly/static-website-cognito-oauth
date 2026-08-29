# Static Website Cognito OAuth

> [!WARNING]
> **AI-authored:** This change was autonomously planned and implemented by an AI software factory from a human-authored specification, with possible subsequent human review or modification.

> [!WARNING]
> This experiment is effectively abandoned. The generated material is retained primarily as a research artifact.

Tests a small static site publishing platform: a Lambda API behind API Gateway with a Cognito JWT authorizer takes a ZIP, unpacks it under `<sub>/<siteId>/` in S3, and CloudFront serves it through Origin Access Control.

```sh
make deploy
make e2e
make destroy
```
