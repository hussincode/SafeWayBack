const http = require('http');

const BASE_URL = 'http://localhost:5143';

function request(method, path, data = null) {
  return new Promise((resolve) => {
    const url = new URL(path, BASE_URL);
    const postData = data ? JSON.stringify(data) : null;
    const options = {
      hostname: url.hostname,
      port: url.port,
      path: url.pathname,
      method: method,
      headers: {
        'Accept': 'application/json',
        ...(postData ? {
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(postData)
        } : {})
      }
    };

    const req = http.request(options, (res) => {
      let body = '';
      res.on('data', chunk => { body += chunk; });
      res.on('end', () => {
        let json = null;
        try { json = JSON.parse(body); } catch (e) { json = body; }
        resolve({ status: res.statusCode, data: json });
      });
    });

    req.on('error', (err) => {
      resolve({ status: 0, error: err.message });
    });

    if (postData) req.write(postData);
    req.end();
  });
}

async function runTests() {
  console.log('==============================================');
  console.log('   SAFEWAY FULL DATABASE & API TEST SUITE');
  console.log('==============================================\n');

  let passed = 0;
  let failed = 0;

  async function test(name, method, path, data = null, expectedStatus = 200) {
    const res = await request(method, path, data);
    const isSuccess = (Array.isArray(expectedStatus) ? expectedStatus.includes(res.status) : res.status === expectedStatus);
    if (isSuccess) {
      console.log(`[PASS] (${res.status}) ${name} -> ${method} ${path}`);
      passed++;
    } else {
      console.log(`[FAIL] (${res.status}) ${name} -> ${method} ${path}`);
      console.log(`       Details:`, JSON.stringify(res.data || res.error));
      failed++;
    }
    return res.data;
  }

  console.log('--- 1. ADMIN USER & ROUTES ---');
  const adminLogin = await test('Admin Login', 'POST', '/api/Auth/login', { uniqueID: 'Admin', password: 'Admin123' });
  await test('Admin Dashboard Summary', 'GET', '/api/Admin/dashboard-summary');
  await test('Admin Stats', 'GET', '/api/Admin/stats');
  await test('Admin Buses', 'GET', '/api/Admin/buses');
  await test('Admin Activities', 'GET', '/api/Admin/activities');
  await test('Admin Students List', 'GET', '/api/Admin/students');
  await test('Admin Drivers List', 'GET', '/api/Admin/drivers');
  await test('Station List', 'GET', '/api/Station/list');
  await test('Route Change Requests - Stations', 'GET', '/api/routechangerequests/stations');
  await test('Route Change Requests - Routes', 'GET', '/api/routechangerequests/routes');

  console.log('\n--- 2. DRIVER USER & ROUTES ---');
  const driverLogin = await test('Driver Login (DRV001)', 'POST', '/api/Auth/login', { uniqueID: 'DRV001', password: 'Driver123' });
  await test('Driver Info (id=2)', 'GET', '/api/Auth/driver-info/2');
  await test('Driver Route (id=2)', 'GET', '/api/Auth/driver-route/2');
  await test('Update Bus Location', 'POST', '/api/BusLocation/update', { latitude: 24.7136, longitude: 46.6753 });
  await test('Get Bus Location', 'GET', '/api/BusLocation/current');

  console.log('\n--- 3. PARENT USER & ROUTES ---');
  const parentLogin = await test('Parent Login (PAR001)', 'POST', '/api/Auth/login', { uniqueID: 'PAR001', password: 'Parent123' });
  await test('Parent Info (id=3)', 'GET', '/api/Auth/parent-info/3');
  await test('Parent Subscriptions (id=3)', 'GET', '/api/Subscription/parent/3');
  await test('Get Student Location', 'GET', '/api/BusLocation/student-current');

  console.log('\n--- 4. STUDENT USER & ROUTES ---');
  const studentLogin = await test('Student Login (STU001)', 'POST', '/api/Auth/login', { uniqueID: 'STU001', password: 'Student123' });
  await test('Student Info (id=4)', 'GET', '/api/Auth/student-info/4');
  await test('Student Subscription (id=4)', 'GET', '/api/Subscription/student/4');
  await test('Student Notifications / Alerts (id=4)', 'GET', '/api/Notifications/user/4');
  await test('Student Station Change Requests (id=4)', 'GET', '/api/Station/requests/4');
  await test('Student Route Change Requests (id=4)', 'GET', '/api/routechangerequests/user/4');
  await test('Update Student Location', 'POST', '/api/BusLocation/student-update', { latitude: 30.0444, longitude: 31.2357 });

  console.log('\n==============================================');
  console.log(`TEST SUMMARY: ${passed} PASSED, ${failed} FAILED (Total: ${passed + failed})`);
  console.log('==============================================');
}

runTests();
