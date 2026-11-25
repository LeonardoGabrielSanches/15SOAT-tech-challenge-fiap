terraform {
  required_providers {
    aws = {
      source = "hashicorp/aws"
    }
    random = {
      source = "hashicorp/random"
    }
  }
}

resource "random_password" "mestre" {
  length  = 32
  special = false
}

resource "aws_security_group" "banco" {
  name        = "oficina-banco"
  description = "PostgreSQL acessivel apenas pelos nodes"
  vpc_id      = var.vpc_id
}

resource "aws_vpc_security_group_ingress_rule" "postgres_dos_nodes" {
  security_group_id            = aws_security_group.banco.id
  referenced_security_group_id = var.security_group_nodes_id
  ip_protocol                  = "tcp"
  from_port                    = 5432
  to_port                      = 5432
}

resource "aws_db_subnet_group" "banco" {
  name       = "oficina"
  subnet_ids = var.subnets_privadas
}

resource "aws_db_instance" "banco" {
  identifier = "oficina"

  engine         = "postgres"
  engine_version = "17"
  instance_class = "db.t4g.micro"

  allocated_storage = 20
  storage_type      = "gp3"
  storage_encrypted = true

  db_name  = var.nome_database
  username = var.usuario_mestre
  password = random_password.mestre.result

  multi_az               = false
  publicly_accessible    = false
  db_subnet_group_name   = aws_db_subnet_group.banco.name
  vpc_security_group_ids = [aws_security_group.banco.id]

  skip_final_snapshot     = true
  deletion_protection     = false
  backup_retention_period = 0
  apply_immediately       = true

  lifecycle {
    ignore_changes = [engine_version]
  }
}
