# 0002 — SeaweedFS instead of MinIO for local object storage

- **Status:** Accepted
- **Date:** 2026-10-31

## Context
The requirements (Stage 3, Stage 9) named MinIO as the S3-compatible store for local development, with AWS S3 in staging/prod. While building the repo skeleton (prompt 01), the `minio/minio` and `minio/mc` images could not be pulled from Docker Hub or quay.io. MinIO stopped publishing free pre-built container images in late 2025, so pinning a MinIO image is no longer a dependable option.

## Decision
Use **SeaweedFS** (`chrislusf/seaweedfs`, Apache-2.0) running `weed server -s3` as the local S3-compatible store, with the AWS CLI image creating the bucket at startup. Dev credentials live in `deploy/seaweedfs/s3.json`.

Application code talks to storage only through `Hrms.Storage` / `IFileStorage` using the plain S3 API (AWS SDK with a configurable service URL), so the choice of local backend never leaks into services. Staging/prod continue to use **AWS S3**.

## Consequences
- Local dev endpoint is `http://localhost:8333` (S3) instead of `:9000`; there is no MinIO-style web console. Inspect buckets with the AWS CLI (`--endpoint-url http://localhost:8333`).
- Storage features used by the app must stay within what both AWS S3 and SeaweedFS support (presigned PUT/GET, basic bucket ops). Object versioning and lifecycle rules are configured on AWS S3 only (Terraform, prompt 19).
- `HRMS-Requirements.md` references to "MinIO (dev)" now mean this S3-compatible dev store.
