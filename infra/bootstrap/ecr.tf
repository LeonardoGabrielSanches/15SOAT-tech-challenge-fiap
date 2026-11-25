resource "aws_ecr_repository" "repositorio" {
  for_each = toset(var.repositorios_ecr)

  name                 = each.value
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }
}

resource "aws_ecr_lifecycle_policy" "repositorio" {
  for_each = aws_ecr_repository.repositorio

  repository = each.value.name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Mantem as ${var.imagens_mantidas} imagens mais recentes"
        selection = {
          tagStatus   = "any"
          countType   = "imageCountMoreThan"
          countNumber = var.imagens_mantidas
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}
