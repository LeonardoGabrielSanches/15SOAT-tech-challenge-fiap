output "vpc_id" {
  value = aws_vpc.principal.id
}

output "subnets_publicas" {
  value = aws_subnet.publica[*].id
}

output "subnets_privadas" {
  value = aws_subnet.privada[*].id
}
