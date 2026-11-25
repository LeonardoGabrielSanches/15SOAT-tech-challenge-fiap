terraform {
  backend "s3" {
    bucket       = "15soat-tech-challenge-fiap-leonardo-sanches"
    key          = "ambiente/terraform.tfstate"
    region       = "us-east-1"
    encrypt      = true
    use_lockfile = true
  }
}
