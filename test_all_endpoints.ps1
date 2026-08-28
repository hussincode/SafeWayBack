$baseUrl = "http://localhost:5143"
[Console]::WriteLine("=== TESTING ALL USERS & ROUTES ===")

function Test-Endpoint {
    param([string]$name, [string]$method, [string]$url, [object]$body = $null)
    try {
        if ($body) {
            $jsonBody = $body | ConvertTo-Json -Depth 5
            $res = Invoke-RestMethod -Uri "$baseUrl$url" -Method $method -Body $jsonBody -ContentType "application/json" -TimeoutSec 10
        } else {
            $res = Invoke-RestMethod -Uri "$baseUrl$url" -Method $method -TimeoutSec 10
        }
        [Console]::WriteLine("[PASS] $name ($method $url)")
        return $res
    } catch {
        [Console]::WriteLine("[FAIL] $name ($method $url) -> $($_.Exception.Message)")
        if ($_.Exception.Response) {
            try {
                $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                [Console]::WriteLine("       Error: $($sr.ReadToEnd())")
            } catch {}
        }
        return $null
    }
}

# 1. Admin Login & Endpoints
[Console]::WriteLine("`n--- 1. ADMIN USER ---")
$adminLogin = Test-Endpoint "Admin Login" "Post" "/api/Auth/login" @{ uniqueID = "Admin"; password = "Admin123" }
$adminSummary = Test-Endpoint "Admin Dashboard Summary" "Get" "/api/Admin/dashboard-summary"
$adminStats = Test-Endpoint "Admin Stats" "Get" "/api/Admin/stats"
$adminBuses = Test-Endpoint "Admin Buses" "Get" "/api/Admin/buses"
$adminActivities = Test-Endpoint "Admin Activities" "Get" "/api/Admin/activities"
$adminStudents = Test-Endpoint "Admin Students" "Get" "/api/Admin/students"
$adminDrivers = Test-Endpoint "Admin Drivers" "Get" "/api/Admin/drivers"
$stationList = Test-Endpoint "Station List" "Get" "/api/Station/list"
$rcrStations = Test-Endpoint "Route/Station Change: Stations" "Get" "/api/routechangerequests/stations"
$rcrRoutes = Test-Endpoint "Route/Station Change: Routes" "Get" "/api/routechangerequests/routes"

# 2. Driver Login & Endpoints
[Console]::WriteLine("`n--- 2. DRIVER USER ---")
$driverLogin = Test-Endpoint "Driver Login" "Post" "/api/Auth/login" @{ uniqueID = "DRV001"; password = "Driver123" }
$driverInfo = Test-Endpoint "Driver Info" "Get" "/api/Auth/driver-info/2"
$driverRoute = Test-Endpoint "Driver Route" "Get" "/api/Auth/driver-route/2"
$busLocUpdate = Test-Endpoint "Update Bus Location" "Post" "/api/BusLocation/update" @{ latitude = 24.7136; longitude = 46.6753 }
$busLocGet = Test-Endpoint "Get Bus Location" "Get" "/api/BusLocation/current"

# 3. Parent Login & Endpoints
[Console]::WriteLine("`n--- 3. PARENT USER ---")
$parentLogin = Test-Endpoint "Parent Login" "Post" "/api/Auth/login" @{ uniqueID = "PAR001"; password = "Parent123" }
$parentInfo = Test-Endpoint "Parent Info" "Get" "/api/Auth/parent-info/3"
$parentSubs = Test-Endpoint "Parent Subscriptions" "Get" "/api/Subscription/parent/3"
$stuLocGet = Test-Endpoint "Get Student Location" "Get" "/api/BusLocation/student-current"

# 4. Student Login & Endpoints
[Console]::WriteLine("`n--- 4. STUDENT USER ---")
$stuLogin = Test-Endpoint "Student Login" "Post" "/api/Auth/login" @{ uniqueID = "STU001"; password = "Student123" }
$stuInfo = Test-Endpoint "Student Info" "Get" "/api/Auth/student-info/4"
$stuSub = Test-Endpoint "Student Subscription" "Get" "/api/Subscription/student/4"
$stuAlerts = Test-Endpoint "Student Alerts/Notifications" "Get" "/api/Notifications/user/4"
$stuRequests = Test-Endpoint "Student Change Requests" "Get" "/api/Station/requests/4"
$stuLocUpdate = Test-Endpoint "Update Student Location" "Post" "/api/BusLocation/student-update" @{ latitude = 30.0444; longitude = 31.2357 }

[Console]::WriteLine("`n=== ALL TESTS COMPLETE ===")
