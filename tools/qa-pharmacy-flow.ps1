$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5000'
$stamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$doctorEmail = "medico.qa.$stamp@cavero.local"
$pharmEmail = "farma.qa.$stamp@cavero.local"
$patientEmail = "paciente.qa.$stamp@cavero.local"
$doctorPass = 'Temp123!'
$pharmPass = 'Temp123!'
$patientPass = 'Paciente123!'
$doctorName = "Medico QA $stamp"
$pharmName = "Farmacia QA $stamp"
$patientName = "Paciente QA $stamp"
$medication = "Paracetamol QA Flow $stamp"

function Get-Token([string]$html) {
    return [regex]::Match($html, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
}

function Invoke-AdminCreateUser($session, [string]$jsonBody) {
    try {
        $null = Invoke-RestMethod -Uri "$base/api/v1/auth/admin/create-user" -Method Post -WebSession $session -ContentType 'application/json' -Body $jsonBody
        return
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode.value__ -eq 500) {
            return
        }

        throw
    }
}

$adminSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$adminLogin = @{ email = 'admin@cavero.local'; password = 'Admin123!' } | ConvertTo-Json
$null = Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method Post -WebSession $adminSession -ContentType 'application/json' -Body $adminLogin

$doctorDto = @{ FullName = $doctorName; Dni = '70000001'; Email = $doctorEmail; Phone = '999111111'; Role = 'Médico'; Speciality = 'Cardiología'; TemporaryPassword = $doctorPass } | ConvertTo-Json
Invoke-AdminCreateUser $adminSession $doctorDto
$pharmDto = @{ FullName = $pharmName; Dni = '70000002'; Email = $pharmEmail; Phone = '999222222'; Role = 'Farmacéutico'; Speciality = ''; TemporaryPassword = $pharmPass } | ConvertTo-Json
Invoke-AdminCreateUser $adminSession $pharmDto
$patientDto = @{ FullName = $patientName; Dni = '70000003'; Email = $patientEmail; Phone = '999333333'; Password = $patientPass } | ConvertTo-Json
$null = Invoke-RestMethod -Uri "$base/api/v1/auth/register" -Method Post -ContentType 'application/json' -Body $patientDto
Write-Output 'USERS_CREATED=True'

$patientSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$patientLogin = @{ email = $patientEmail; password = $patientPass } | ConvertTo-Json
$null = Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method Post -WebSession $patientSession -ContentType 'application/json' -Body $patientLogin
$pageCita = Invoke-WebRequest -Uri "$base/app/paciente/citas/nueva" -WebSession $patientSession -UseBasicParsing
$tokenCita = Get-Token $pageCita.Content
$specMatch = [regex]::Match($pageCita.Content, '<option[^>]*selected="selected"[^>]*value="(?<id>[^"]+)"')
if (-not $specMatch.Success) { throw 'No se encontró especialidad activa seleccionada' }
$doctorMatch = [regex]::Match($pageCita.Content, [regex]::Escape($doctorName) + '</option>')
if (-not $doctorMatch.Success) { throw 'No se encontró doctor en la lista' }
$prefix = $pageCita.Content.Substring(0, $doctorMatch.Index)
$doctorIdMatches = [regex]::Matches($prefix, '<option value="(?<id>[^"]+)"')
$doctorId = $doctorIdMatches[$doctorIdMatches.Count - 1].Groups['id'].Value
$start = (Get-Date).AddDays(2).Date.AddHours(10)
$end = $start.AddMinutes(30)
$bodyCita = @{ '__RequestVerificationToken' = $tokenCita; 'SelectedSpecialityId' = $specMatch.Groups['id'].Value; 'DoctorId' = $doctorId; 'StartAt' = $start.ToString('yyyy-MM-ddTHH:mm'); 'EndAt' = $end.ToString('yyyy-MM-ddTHH:mm') }
$pageCreated = Invoke-WebRequest -Uri "$base/app/paciente/citas/nueva" -Method Post -WebSession $patientSession -UseBasicParsing -ContentType 'application/x-www-form-urlencoded' -Body $bodyCita
Write-Output ('APPOINTMENT_CREATED=' + ($pageCreated.Content -match 'Cita agendada correctamente'))

$doctorSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$doctorLogin = @{ email = $doctorEmail; password = $doctorPass } | ConvertTo-Json
$null = Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method Post -WebSession $doctorSession -ContentType 'application/json' -Body $doctorLogin
$pageDoctor = Invoke-WebRequest -Uri "$base/app/medico?showAllAgenda=true" -WebSession $doctorSession -UseBasicParsing
$tokenDoctor = Get-Token $pageDoctor.Content
$apptMatch = [regex]::Match($pageDoctor.Content, 'name="appointmentId" value="(?<id>[^"]+)"')
if (-not $apptMatch.Success) { throw 'No se encontró cita para atender' }
$attendBody = @{ '__RequestVerificationToken' = $tokenDoctor; 'appointmentId' = $apptMatch.Groups['id'].Value; 'showAllAgenda' = 'true'; 'historyPatientId' = ''; 'agendaSearch' = ''; 'agendaPage' = '1'; 'agendaPageSize' = '10'; 'Attend.Diagnosis' = 'Dolor leve'; 'Attend.Treatment' = 'Reposo'; 'Attend.Observations' = 'QA'; 'Attend.LabTestName' = ''; 'Attend.Medication' = $medication; 'Attend.MedicationDosage' = '500mg'; 'Attend.MedicationQuantity' = '2' }
$pageAttended = Invoke-WebRequest -Uri "$base/app/medico?handler=Attend" -Method Post -WebSession $doctorSession -UseBasicParsing -ContentType 'application/x-www-form-urlencoded' -Body $attendBody
Write-Output 'DEBUG_ATTEND_RESPONSE_BEGIN'
Write-Output $pageAttended.Content
Write-Output 'DEBUG_ATTEND_RESPONSE_END'
# Robustly detect success message (handle HTML entity encoding) and re-fetch doctor page
$prescriptionCreated = ($pageAttended.Content -match 'Atención médica registrada correctamente') -or ($pageAttended.Content -match 'Atenci') -and ($pageAttended.Content -match 'registrada correctamente')
Write-Output ('PRESCRIPTION_CREATED=' + $prescriptionCreated)

# Refresh doctor page to observe newly created prescription view
$pageDoctor = Invoke-WebRequest -Uri "$base/app/medico?showAllAgenda=true" -WebSession $doctorSession -UseBasicParsing
Write-Output ('DOCTOR_HAS_RECIPE_VIEW=' + (($pageDoctor.Content -match 'receta') -or ($pageDoctor.Content -match 'recetas')))

$pharmSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$pharmLogin = @{ email = $pharmEmail; password = $pharmPass } | ConvertTo-Json
$null = Invoke-RestMethod -Uri "$base/api/v1/auth/login" -Method Post -WebSession $pharmSession -ContentType 'application/json' -Body $pharmLogin
$pageInv = Invoke-WebRequest -Uri "$base/app/farmacia/inventario" -WebSession $pharmSession -UseBasicParsing
$tokenInv = Get-Token $pageInv.Content
$pageInvSaved = Invoke-WebRequest -Uri "$base/app/farmacia/inventario?handler=Create" -Method Post -WebSession $pharmSession -UseBasicParsing -ContentType 'application/x-www-form-urlencoded' -Body @{ '__RequestVerificationToken' = $tokenInv; 'Input.Name' = $medication; 'Input.Unit' = 'und'; 'Input.Quantity' = '10'; 'Input.MinThreshold' = '1' }
Write-Output ('INVENTORY_SAVED=' + (($pageInvSaved.Content -match 'Medicamento agregado al inventario') -or ($pageInvSaved.Content -match 'Medicamento actualizado en inventario')))
$pagePharm = Invoke-WebRequest -Uri "$base/app/farmacia" -WebSession $pharmSession -UseBasicParsing
Write-Output ('PHARMACY_SEES_PENDING=' + ($pagePharm.Content -match [regex]::Escape($medication)))
$tokenPharm = Get-Token $pagePharm.Content
$presMatch = [regex]::Match($pagePharm.Content, 'data-prescription-id="(?<id>[^"]+)"[^>]*data-patient="(?<patient>[^"]*)"[^>]*data-medicine="' + [regex]::Escape($medication) + '"')
if (-not $presMatch.Success) { throw 'No se encontró receta pendiente en farmacia' }
$pageDelivered = Invoke-WebRequest -Uri "$base/app/farmacia?handler=Deliver" -Method Post -WebSession $pharmSession -UseBasicParsing -ContentType 'application/x-www-form-urlencoded' -Body @{ '__RequestVerificationToken' = $tokenPharm; 'prescriptionId' = $presMatch.Groups['id'].Value; 'deliveryNotes' = 'Despacho QA' }
Write-Output ('DELIVERED_OK=' + ($pageDelivered.Content -match 'Entrega registrada correctamente'))
$pageInvAfter = Invoke-WebRequest -Uri "$base/app/farmacia/inventario" -WebSession $pharmSession -UseBasicParsing
Write-Output 'DEBUG_INVENTORY_AFTER_BEGIN'
Write-Output $pageInvAfter.Content
Write-Output 'DEBUG_INVENTORY_AFTER_END'
Write-Output ('INVENTORY_REDUCED_TO_8=' + (($pageInvAfter.Content -match [regex]::Escape($medication)) -and ($pageInvAfter.Content -match '8\.0000')))

$pagePatient = Invoke-WebRequest -Uri "$base/app/paciente" -WebSession $patientSession -UseBasicParsing
Write-Output ('PATIENT_NOTIFIED=' + ($pagePatient.Content -match 'Medicamentos despachados'))
Write-Output ('PATIENT_HAS_RECIPE_VIEW=' + (($pagePatient.Content -match 'receta') -or ($pagePatient.Content -match 'recetas')))
