data "aws_caller_identity" "atual" {}

resource "aws_iam_openid_connect_provider" "github" {
  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]
}

data "aws_iam_policy_document" "confianca_ci" {
  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github.arn]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:sub"
      values   = [for branch in var.branches_permitidas : "${var.prefixo_sub_oidc}:ref:refs/heads/${branch}"]
    }
  }
}

resource "aws_iam_role" "ci" {
  name                 = "oficina-ci"
  assume_role_policy   = data.aws_iam_policy_document.confianca_ci.json
  max_session_duration = 7200
}

data "aws_iam_policy_document" "permissoes_ci" {
  statement {
    sid = "ServicosDasStacks"
    actions = [
      "ec2:*",
      "eks:*",
      "rds:*",
      "elasticloadbalancing:*",
      "autoscaling:*",
      "logs:*",
      "kms:Describe*",
      "kms:List*",
      "kms:CreateGrant",
      "kms:Decrypt",
      "kms:GenerateDataKey*"
    ]
    resources = ["*"]
  }

  statement {
    sid       = "EcrToken"
    actions   = ["ecr:GetAuthorizationToken"]
    resources = ["*"]
  }

  statement {
    sid       = "EcrRepositorios"
    actions   = ["ecr:*"]
    resources = ["arn:aws:ecr:${var.regiao}:${data.aws_caller_identity.atual.account_id}:repository/oficina-*"]
  }

  statement {
    sid       = "StateListagem"
    actions   = ["s3:ListBucket", "s3:GetBucketLocation", "s3:GetBucketVersioning"]
    resources = [aws_s3_bucket.state.arn]
  }

  statement {
    sid       = "StateObjetos"
    actions   = ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"]
    resources = ["${aws_s3_bucket.state.arn}/*"]
  }

  statement {
    sid = "IamComPrefixoOficina"
    actions = [
      "iam:CreateRole",
      "iam:DeleteRole",
      "iam:GetRole",
      "iam:UpdateRole",
      "iam:TagRole",
      "iam:UntagRole",
      "iam:ListRolePolicies",
      "iam:ListAttachedRolePolicies",
      "iam:ListInstanceProfilesForRole",
      "iam:AttachRolePolicy",
      "iam:DetachRolePolicy",
      "iam:PutRolePolicy",
      "iam:GetRolePolicy",
      "iam:DeleteRolePolicy",
      "iam:UpdateAssumeRolePolicy",
      "iam:PassRole",
      "iam:CreatePolicy",
      "iam:DeletePolicy",
      "iam:GetPolicy",
      "iam:GetPolicyVersion",
      "iam:ListPolicyVersions",
      "iam:CreatePolicyVersion",
      "iam:DeletePolicyVersion",
      "iam:TagPolicy",
      "iam:UntagPolicy",
      "iam:CreateInstanceProfile",
      "iam:DeleteInstanceProfile",
      "iam:GetInstanceProfile",
      "iam:AddRoleToInstanceProfile",
      "iam:RemoveRoleFromInstanceProfile",
      "iam:TagInstanceProfile"
    ]
    resources = [
      "arn:aws:iam::${data.aws_caller_identity.atual.account_id}:role/oficina-*",
      "arn:aws:iam::${data.aws_caller_identity.atual.account_id}:policy/oficina-*",
      "arn:aws:iam::${data.aws_caller_identity.atual.account_id}:instance-profile/oficina-*"
    ]
  }

  statement {
    sid       = "PapeisVinculadosAServicos"
    actions   = ["iam:CreateServiceLinkedRole"]
    resources = ["*"]

    condition {
      test     = "StringEquals"
      variable = "iam:AWSServiceName"
      values = [
        "eks.amazonaws.com",
        "eks-nodegroup.amazonaws.com",
        "elasticloadbalancing.amazonaws.com",
        "autoscaling.amazonaws.com",
        "rds.amazonaws.com"
      ]
    }
  }
}

resource "aws_iam_role_policy" "ci" {
  name   = "oficina-ci"
  role   = aws_iam_role.ci.id
  policy = data.aws_iam_policy_document.permissoes_ci.json
}
