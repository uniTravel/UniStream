{{/*
Expand the name of the chart.
*/}}
{{- define "eventstore.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "eventstore.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "eventstore.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "eventstore.labels" -}}
helm.sh/chart: {{ include "eventstore.chart" . }}
{{ include "eventstore.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "eventstore.selectorLabels" -}}
app.kubernetes.io/name: {{ include "eventstore.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "eventstore.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "eventstore.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
Generate the EVENTSTORE_GOSSIP_SEED string from a list of member names.
*/}}
{{- define "eventstore.gossipSeeds" -}}
{{- $seeds := list }}
{{- $name := include "eventstore.fullname" . }}
{{- range $i := until (int .Values.replicaCount) }}
  {{- $seed := printf "%s-%d.%s-headless.default.svc.cluster.local:2113" $name $i $name }}
  {{- $seeds = append $seeds $seed }}
{{- end }}
{{- join "," $seeds }}
{{- end }}
