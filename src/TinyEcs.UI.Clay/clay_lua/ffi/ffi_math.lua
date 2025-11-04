-- Custom math library for 3D computations

local ffi = require("ffi")
ffi.cdef([[
typedef struct { float x, y, z; } vec2_t;
typedef struct { float x, y, z; } vec3_t;
typedef struct { float x, y, z, w; } vec4_t;
typedef struct { float x, y, z, w; } quat_t;
typedef union {
	float m[16];
	struct {	// row-major
		
		float r00, r01, r02, r03;
		float r10, r11, r12, r13;
		float r20, r21, r22, r23;
		float r30, r31, r32, r33;
	};
	struct {	// column-major
		float c00, c10, c20, c30;
		float c01, c11, c21, c31;
		float c02, c12, c22, c32;
		float c03, c13, c23, c33;
	};
} mat4_t;

double round(double);
]])


-- vec2
local vec2_mt = { __index = {} }
local vec2 = vec2_mt.__index

function vec2_mt.__call(self,x,y)
	self.x = x or 0
	self.y = y or 0
	return self
end

function vec2.new(x, y, z, w)
	return ffi.new("vec2_t", x or 0, y or 0)
end

function vec2.add(self,x,y)
	return ffi.new("vec2_t",self.x+x,self.y+y)
end

function vec2.sub(self,x,y)
	return ffi.new("vec2_t",self.x-x,self.y-y)
end

function vec2.mul(self,x,y)
	return ffi.new("vec2_t",self.x*x,self.y*y)
end

function vec2.div(self,x,y)
	return ffi.new("vec2_t",self.x/x,self.y/y)
end

function vec2.clone(self)
	return ffi.new("vec2_t",self.x,self.y)
end

function vec2.unpack(self)
	return self.x,self.y
end

-- vec3
local vec3_mt = { __index = {} }
local vec3 = vec3_mt.__index

function vec3_mt.__call(self,x,y,z)
	self.x = x or 0
	self.y = y or 0
	self.z = z or 0
	return self
end

function vec3.new(x, y, z)
	return ffi.new("vec3_t", x or 0, y or 0, z or 0)
end

--@usage: a:add(b:unpack())
--@returns new vec3_t
function vec3.add(self,x,y,z)
	return ffi.new("vec3_t",self.x+x,self.y+y,self.z+z)
end
vec3_mt.__add = function(a,b) return vec3.add(a,b:unpack()) end

--@usage:  a:sub(b:unpack())
function vec3.sub(self,x,y,z)
	return ffi.new("vec3_t",self.x-x,self.y-y,self.z-z)
end
vec3_mt.__sub = function(a,b) return vec3.sub(a,b:unpack()) end

--@usage: a:mul(b:unpack())
function vec3.mul(self,x,y,z)
	return ffi.new("vec3_t",self.x*x,self.y*y,self.z*z)
end
vec3_mt.__mul = function(a,b) return vec3.mul(a,b:unpack()) end

--@usage: a:div(b:unpack())
function vec3.div(self,x,y,z)
	return ffi.new("vec3_t",self.x/x,self.y/y,self.z/z)
end
vec3_mt.__div = function(a,b) return vec3.div(a,b:unpack()) end

function vec3.cross(self,x,y,z)
	return ffi.new("vec3_t",self.y*z - self.z*y, self.z*x - self.x*z, self.x*y - self.y*x)
end

function vec3.dot(self,x,y,z)
	return self.x*x + self.y*y + self.z*z
end

function vec3.length(self)
	return math.sqrt(self.x * self.x + self.y * self.y + self.z * self.z)
end

function vec3.length2(self)
	return self.x * self.x + self.y * self.y + self.z * self.z
end

function vec3.distance(self,x,y,z)
	local dx = self.x-x
	local dy = self.y-y
	local dz = self.z-z
	return math.sqrt(dx * dx + dy * dy + dz * dz)
end

function vec3.distance2(self,x,y,z)
	local dx = self.x-x
	local dy = self.y-y
	local dz = self.z-z
	return dx * dx + dy * dy + dz * dz
end

function vec3.scale(self,s)
	return ffi.new("vec3_t",self.x*s,self.y*s,self.z*s)
end

function vec3.normalize(self)
	if self:is_zero() then
		return ffi.new("vec3_t")
	end
	local len = self:length()
	if (len > 0) then
		return self:clone():scale(1 / len)
	end
	return ffi.new("vec3_t")
end

--@usage: local b = a:clone()
function vec3.clone(self)
	return ffi.new("vec3_t",self.x,self.y,self.z)
end

function vec3.unpack(self)
	return self.x,self.y,self.z
end

function vec3.is_zero(self)
	return self.x == 0 and self.y == 0 and self.z == 0
end

function vec3.lerp(a, b, t)
	return ffi.new("vec3_t",
		a.x + (b.x - a.x) * t,
		a.y + (b.y - a.y) * t,
		a.z + (b.z - a.z) * t
	)
end

-- vec4
local vec4_mt = { __index = {} }
local vec4 = vec4_mt.__index

function vec4_mt.__call(self, x, y, z, w)
	self.x = x or 0
	self.y = y or 0
	self.z = z or 0
	self.w = w or 0
	return self
end

function vec4.new(x, y, z, w)
	return ffi.new("vec4_t", x or 0, y or 0, z or 0, w or 0)
end

function vec4.add(self, x, y, z, w)
	return ffi.new("vec4_t", self.x + x, self.y + y, self.z + z, self.w + w)
end
vec4_mt.__add = function(a, b) return vec4.add(a, b:unpack()) end

function vec4.sub(self, x, y, z, w)
	return ffi.new("vec4_t", self.x - x, self.y - y, self.z - z, self.w - w)
end
vec4_mt.__sub = function(a, b) return vec4.sub(a, b:unpack()) end

function vec4.mul(self, x, y, z, w)
	return ffi.new("vec4_t", self.x * x, self.y * y, self.z * z, self.w * w)
end
vec4_mt.__mul = function(a, b) return vec4.mul(a, b:unpack()) end

function vec4.scale(self, s)
	return ffi.new("vec4_t", self.x * s, self.y * s, self.z * s, self.w * s)
end

function vec4.dot(self, x, y, z, w)
	return self.x * x + self.y * y + self.z * z + self.w * w
end

function vec4.length(self)
	return math.sqrt(self.x^2 + self.y^2 + self.z^2 + self.w^2)
end

function vec4.length2(self)
	return self.x^2 + self.y^2 + self.z^2 + self.w^2
end

function vec4.normalize(self)
	local len = self:length()
	if len == 0 then return ffi.new("vec4_t") end
	return self:scale(1 / len)
end

function vec4.clone(self)
	return ffi.new("vec4_t", self.x, self.y, self.z, self.w)
end

function vec4.unpack(self)
	return self.x, self.y, self.z, self.w
end

function vec4.is_zero(self)
	return self.x == 0 and self.y == 0 and self.z == 0 and self.w == 0
end

function vec4.lerp(a, b, t)
	return ffi.new("vec4_t",
		a.x + (b.x - a.x) * t,
		a.y + (b.y - a.y) * t,
		a.z + (b.z - a.z) * t,
		a.w + (b.w - a.w) * t
	)
end

-- quat
local quat_mt = { __index = {} }
local quat = quat_mt.__index

function quat_mt.__call(self,x,y,z)
	self.x = x or 0
	self.y = y or 0
	self.z = z or 0
    self.w = w or 0
	return self
end

function quat.new(x,y,z,w)
    return ffi.new("quat_t",x or 0, y or 0, z or 0, w or 0)
end

function quat.add(self,x,y,z,w)
	return ffi.new("quat_t",self.x+x,self.y+y,self.z+z,self.w+w)
end
quat_mt.__add = function(a,b) return quat.add(a,b:unpack()) end

function quat.sub(self,x,y,z,w)
	return ffi.new("quat_t",self.x-x,self.y-y,self.z-z,self.w-w)
end
quat_mt.__sub = function(a,b) return quat.sub(a,b:unpack()) end

function quat.mul(self,x,y,z,w)
	return ffi.new("quat_t",self.x*w+self.w*x+self.y*z-self.z*y,self.y*w+self.w*y+self.z*x-self.x*z,self.z*w+self.w*z+self.x*y-self.y*x,self.w*w-self.x*x-self.y*y-self.z*z)
end
quat_mt.__mul = function(a,b) return quat.mul(a,b:unpack()) end

function quat.scale(self,s)
	return ffi.new("quat_t",self.x*s,self.y*s,self.z*s,self.w*s)
end

function quat.toMat4(self)
	local x, y, z, w = self.x, self.y, self.z, self.w
	local xx, yy, zz = x*x, y*y, z*z
	local xy, xz, yz = x*y, x*z, y*z
	local wx, wy, wz = w*x, w*y, w*z

	return ffi.new("mat4_t")(
		1 - 2*(yy + zz), 2*(xy - wz),     2*(xz + wy),     0,
		2*(xy + wz),     1 - 2*(xx + zz), 2*(yz - wx),     0,
		2*(xz - wy),     2*(yz + wx),     1 - 2*(xx + yy), 0,
		0,               0,               0,               1
	)
end

function quat.lerp(a, b, t)
	return ffi.new("quat_t",
		a.x + (b.x - a.x) * t,
		a.y + (b.y - a.y) * t,
		a.z + (b.z - a.z) * t,
		a.w + (b.w - a.w) * t
	):normalize()
end

function quat.slerp(a, b, t)
	local dot = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w
	local b_adj

	if dot < 0 then
		b_adj = ffi.new("quat_t", -b.x, -b.y, -b.z, -b.w)
		dot = -dot
	else
		b_adj = b
	end

	dot = math.min(math.max(dot, -1), 1)

	if dot > 0.9995 then
		local q = ffi.new("quat_t",
			a.x + t*(b_adj.x - a.x),
			a.y + t*(b_adj.y - a.y),
			a.z + t*(b_adj.z - a.z),
			a.w + t*(b_adj.w - a.w)
		)
		return q:normalize()
	end

	local theta_0 = math.acos(dot)
	local theta = theta_0 * t
	local sin_theta = math.sin(theta)
	local sin_theta_0 = math.sin(theta_0)

	local s0 = math.cos(theta) - dot * sin_theta / sin_theta_0
	local s1 = sin_theta / sin_theta_0

	return ffi.new("quat_t",
		s0 * a.x + s1 * b_adj.x,
		s0 * a.y + s1 * b_adj.y,
		s0 * a.z + s1 * b_adj.z,
		s0 * a.w + s1 * b_adj.w
	):normalize()
end


function quat.dot(self,other)
    return self.x * other.x + self.y * other.y + self.z * other.z + self.w * other.w
end

function quat.negate(self)
    return ffi.new("quat_t", -self.x, -self.y, -self.z, -self.w)
end

function quat.from_mat4(m)
	local trace = m.r00 + m.r11 + m.r22
	local q = ffi.new("quat_t")

	if trace > 0 then
		local s = 0.5 / math.sqrt(trace + 1.0)
		q.w = 0.25 / s
		q.x = (m.r21 - m.r12) * s
		q.y = (m.r02 - m.r20) * s
		q.z = (m.r10 - m.r01) * s
	elseif (m.r00 > m.r11 and m.r00 > m.r22) then
		local s = 2.0 * math.sqrt(1.0 + m.r00 - m.r11 - m.r22)
		q.w = (m.r21 - m.r12) / s
		q.x = 0.25 * s
		q.y = (m.r01 + m.r10) / s
		q.z = (m.r02 + m.r20) / s
	elseif m.r11 > m.r22 then
		local s = 2.0 * math.sqrt(1.0 + m.r11 - m.r00 - m.r22)
		q.w = (m.r02 - m.r20) / s
		q.x = (m.r01 + m.r10) / s
		q.y = 0.25 * s
		q.z = (m.r12 + m.r21) / s
	else
		local s = 2.0 * math.sqrt(1.0 + m.r22 - m.r00 - m.r11)
		q.w = (m.r10 - m.r01) / s
		q.x = (m.r02 + m.r20) / s
		q.y = (m.r12 + m.r21) / s
		q.z = 0.25 * s
	end

	return q
end

function quat.from_look(forward, up)
	local z = forward:normalize()
	local x = up:cross(z:unpack()):normalize()
	local y = z:cross(x:unpack())

	local m = ffi.new("mat4_t")(
		x.x, y.x, z.x, 0,
		x.y, y.y, z.y, 0,
		x.z, y.z, z.z, 0,
		0,   0,   0,   1
	)

	return quat.from_mat4(m)
end

function quat.normalize(self)
	local len = math.sqrt(self.x*self.x + self.y*self.y + self.z*self.z + self.w*self.w)
	if len == 0 then return ffi.new("quat_t", 0, 0, 0, 1) end
	return ffi.new("quat_t", self.x/len, self.y/len, self.z/len, self.w/len)
end

function quat.clone(self)
	return ffi.new("quat_t",self.x,self.y,self.z,self.w)
end

function quat.unpack(self)
	return self.x,self.y,self.z,self.w
end


-- mat4
local mat4_mt = { __index = {} }
local mat4 = mat4_mt.__index

function mat4_mt.__call(self,r00,r01,r02,r03,r10,r11,r12,r13,r20,r21,r22,r23,r30,r31,r32,r33)
	self.r00 = r00 or 1
	self.r01 = r01 or 0
	self.r02 = r02 or 0
	self.r03 = r03 or 0
	self.r10 = r10 or 0
	self.r11 = r11 or 1
	self.r12 = r12 or 0
	self.r13 = r13 or 0
	self.r20 = r20 or 0
	self.r21 = r21 or 0
	self.r22 = r22 or 1
	self.r23 = r23 or 0
	self.r30 = r30 or 0
	self.r31 = r31 or 0
	self.r32 = r32 or 0
	self.r33 = r33 or 1
	return self
end

function mat4.new(r00,r01,r02,r03,r10,r11,r12,r13,r20,r21,r22,r23,r30,r31,r32,r33)
    local self = ffi.new("mat4_t")
	self.r00 = r00 or 1
	self.r01 = r01 or 0
	self.r02 = r02 or 0
	self.r03 = r03 or 0
	self.r10 = r10 or 0
	self.r11 = r11 or 1
	self.r12 = r12 or 0
	self.r13 = r13 or 0
	self.r20 = r20 or 0
	self.r21 = r21 or 0
	self.r22 = r22 or 1
	self.r23 = r23 or 0
	self.r30 = r30 or 0
	self.r31 = r31 or 0
	self.r32 = r32 or 0
	self.r33 = r33 or 1
	return self
end

function mat4.identity(self)
	self.r00 = 1
	self.r01 = 0
	self.r02 = 0
	self.r03 = 0
	self.r10 = 0
	self.r11 = 1
	self.r12 = 0
	self.r13 = 0
	self.r20 = 0
	self.r21 = 0
	self.r22 = 1
	self.r23 = 0
	self.r30 = 0
	self.r31 = 0
	self.r32 = 0
	self.r33 = 1
	return self
end

function mat4.mul(a, b)
	local out = ffi.new("mat4_t")

	out.r00 = a.r00*b.r00 + a.r01*b.r10 + a.r02*b.r20 + a.r03*b.r30
	out.r01 = a.r00*b.r01 + a.r01*b.r11 + a.r02*b.r21 + a.r03*b.r31
	out.r02 = a.r00*b.r02 + a.r01*b.r12 + a.r02*b.r22 + a.r03*b.r32
	out.r03 = a.r00*b.r03 + a.r01*b.r13 + a.r02*b.r23 + a.r03*b.r33

	out.r10 = a.r10*b.r00 + a.r11*b.r10 + a.r12*b.r20 + a.r13*b.r30
	out.r11 = a.r10*b.r01 + a.r11*b.r11 + a.r12*b.r21 + a.r13*b.r31
	out.r12 = a.r10*b.r02 + a.r11*b.r12 + a.r12*b.r22 + a.r13*b.r32
	out.r13 = a.r10*b.r03 + a.r11*b.r13 + a.r12*b.r23 + a.r13*b.r33

	out.r20 = a.r20*b.r00 + a.r21*b.r10 + a.r22*b.r20 + a.r23*b.r30
	out.r21 = a.r20*b.r01 + a.r21*b.r11 + a.r22*b.r21 + a.r23*b.r31
	out.r22 = a.r20*b.r02 + a.r21*b.r12 + a.r22*b.r22 + a.r23*b.r32
	out.r23 = a.r20*b.r03 + a.r21*b.r13 + a.r22*b.r23 + a.r23*b.r33

	out.r30 = a.r30*b.r00 + a.r31*b.r10 + a.r32*b.r20 + a.r33*b.r30
	out.r31 = a.r30*b.r01 + a.r31*b.r11 + a.r32*b.r21 + a.r33*b.r31
	out.r32 = a.r30*b.r02 + a.r31*b.r12 + a.r32*b.r22 + a.r33*b.r32
	out.r33 = a.r30*b.r03 + a.r31*b.r13 + a.r32*b.r23 + a.r33*b.r33

	return out
end

mat4_mt.__mul = function (a,b) return mat4.mul(a,b) end

function mat4.translation(self,x,y,z)
	local b = ffi.new("mat4_t"):identity()
	b.r30 = x
	b.r31 = y
	b.r32 = z
	return b
end

function mat4.translate(self,x,y,z)
    local out = self:clone()
    out.r30 = out.r00 * x + out.r10 * y + out.r20 * z + out.r30
    out.r31 = out.r01 * x + out.r11 * y + out.r21 * z + out.r31
    out.r32 = out.r02 * x + out.r12 * y + out.r22 * z + out.r32
    out.r33 = out.r03 * x + out.r13 * y + out.r23 * z + out.r33
    return out
end

function mat4.transpose(self)
	return ffi.new("mat4_t")(self.r00,self.r10,self.r20,self.r30,self.r01,self.r11,self.r21,self.r31,self.r02,self.r12,self.r22,self.r32,self.r03,self.r13,self.r23,self.r33)
end

function mat4.scale(self,x,y,z)
    local out = self:clone()
	out.r00 = x
	out.r11 = y
	out.r22 = z
	return out
end

function mat4.mul_vec4(mat, vec)
	local x = mat.r00 * vec.x + mat.r01 * vec.y + mat.r02 * vec.z + mat.r03 * vec.w
	local y = mat.r10 * vec.x + mat.r11 * vec.y + mat.r12 * vec.z + mat.r13 * vec.w
	local z = mat.r20 * vec.x + mat.r21 * vec.y + mat.r22 * vec.z + mat.r23 * vec.w
	local w = mat.r30 * vec.x + mat.r31 * vec.y + mat.r32 * vec.z + mat.r33 * vec.w
	return ffi.new("vec4_t",x, y, z, w)
end

function mat4.from_ortho(self,left,right,bottom,top,near,far,offset,is_homogeneousNDC,is_right_handed)
	local aa = 2 / (right - left )
	local bb = 2 / (top - bottom)
	local cc = (is_homogeneousNDC and 2 or 1) / (far - near)
	local dd = (left + right) / (left - right)
	local ee = (top + bottom) / (bottom - top)
	local ff = is_homogeneousNDC and (near + far) / (near - far) or (near / (near - far))
	
	self.r00 = aa
	self.r11 = bb
	self.r22 = is_right_handed and -cc or cc
	self.r30 = dd + offset
	self.r31 = ee
	self.r32 = ff
	self.r33 = 1
	
	return self
end

function mat4.look_at(self,eye,center,up,is_right_handed)
    local zaxis = is_right_handed and (eye - center) or (center - eye):normalize()
    local xaxis = up:cross(zaxis:unpack()):normalize()
    local yaxis = zaxis:cross(xaxis:unpack())
    self.r00, self.r01, self.r02, self.r03 = xaxis.x, yaxis.x, zaxis.x, 0
    self.r10, self.r11, self.r12, self.r13 = xaxis.y, yaxis.y, zaxis.y, 0
    self.r20, self.r21, self.r22, self.r23 = xaxis.z, yaxis.z, zaxis.z, 0
    self.r30, self.r31, self.r32, self.r33 = -xaxis:dot(eye:unpack()), -yaxis:dot(eye:unpack()), -zaxis:dot(eye:unpack()), 1
    return self
end

function mat4.from_perspective(self,fovy,aspect,near,far,is_homogeneousNDC,is_right_handed)
	local x,y = 0,0
	local height = 1/math.tan(math.rad(fovy)*0.5)
	local width = height * 1/aspect
	local diff = far-near
	local aa = is_homogeneousNDC and (far + near)/diff or far/diff
	local bb = is_homogeneousNDC and (2*far*near)/diff or near*aa
	
	self:identity()
	
	self.r00 = width
	self.r11 = height
	self.r20 = is_right_handed and x or -x
	self.r21 = is_right_handed and y or -y
	self.r22 = is_right_handed and -aa or aa
	self.r23 = is_right_handed and -1 or 1
	self.r32 = -bb
	
	return self
end

function mat4.look_at_rotation(eye, target, up)
	local forward = (target - eye):normalize()
	local right = up:cross(forward:unpack()):normalize()
	local new_up = forward:cross(right:unpack())

	local m = ffi.new("mat4_t")(
		right.x, new_up.x, forward.x, 0,
		right.y, new_up.y, forward.y, 0,
		right.z, new_up.z, forward.z, 0,
		0,       0,        0,         1
	)

	return m
end

function mat4.mul_pos_w(self, x, y, z)
	local vx = self.r00 * x + self.r10 * y + self.r20 * z + self.r30
	local vy = self.r01 * x + self.r11 * y + self.r21 * z + self.r31
	local vz = self.r02 * x + self.r12 * y + self.r22 * z + self.r32
	local vw = self.r03 * x + self.r13 * y + self.r23 * z + self.r33
	return ffi.new("vec3_t", vx / vw, vy / vw, vz / vw), vw
end

function mat4.mul_pos(self, x, y, z)
	local vx = self.r00 * x + self.r10 * y + self.r20 * z + self.r30
	local vy = self.r01 * x + self.r11 * y + self.r21 * z + self.r31
	local vz = self.r02 * x + self.r12 * y + self.r22 * z + self.r32
	local vw = self.r03 * x + self.r13 * y + self.r23 * z + self.r33

	if vw ~= 0 and vw ~= 1 then
		return ffi.new("vec3_t", vx / vw, vy / vw, vz / vw)
	end

	return ffi.new("vec3_t", vx, vy, vz)
end

function mat4.mul_dir(self, x, y, z, w)
	local out = ffi.new("vec3_t",
		self.r00 * x + self.r10 * y + self.r20 * z,
		self.r01 * x + self.r11 * y + self.r21 * z,
		self.r02 * x + self.r12 * y + self.r22 * z
	)

	-- optional w projection
	if w and w ~= 0 and w ~= 1 then
		return ffi.new("vec3_t", out.x / w, out.y / w, out.z / w)
	end

	return out
end


function mat4.rotateX(self,ax)
	local sx = math.sin(ax)
	local cx = math.cos(ax)
	self.r00 = 1
	self.r11 = cx
	self.r12 = -sx
	self.r21 = sx
	self.r22 = cx
	self.r33 = 1
	return self
end

function mat4.rotateY(self,ay)
	local sy = math.sin(ay)
	local cy = math.cos(ay)
	self.r00 = cy
	self.r02 = sy
	self.r11 = 1
	self.r20 = -sy
	self.r22 = cy
	self.r33 = 1
	return self
end

function mat4.rotateZ(self,az)
	local sz = math.sin(az)
	local cz = math.cos(az)
	self.r00 = cz
	self.r01 = -sz
	self.r10 = sz
	self.r11 = cz
	self.r22 = 1
	self.r33 = 1
	return self
end

function mat4.rotateXY(self,ax,ay)
	local sx = math.sin(ax)
	local cx = math.cos(ax)
	local sy = math.sin(ay)
	local cy = math.cos(ay)
	self.r00 = cy
	self.r02 = sy
	self.r10 = sx*sy
	self.r11 = cx
	self.r12 = -sx*cy
	self.r20 = -cx*sy
	self.r21 = sx
	self.r22 = cx*cy
	self.r33 = 1
	return self
end

function mat4.rotateXYZ(self,ax,ay,az)
	local sx = math.sin(ax)
	local cx = math.cos(ax)
	local sy = math.sin(ay)
	local cy = math.cos(ay)
	local sz = math.sin(az)
	local cz = math.cos(az)
	self.r00 = cy*cz
	self.r01 = -cy*sz
	self.r02 = sy
	self.r10 = cz*sx*sy + cx*sz
	self.r11 = cx*cz - sx*sy*sz
	self.r12 = -cy*sx
	self.r20 = -cx*cz*sy + sx*sz
	self.r21 = cz*sx + cx*sy*sz
	self.r22 = cx*cy
	self.r33 = 1
	return self
end

function mat4.rotateZYX(self,az,ay,ax)
	local sx = math.sin(ax)
	local cx = math.cos(ax)
	local sy = math.sin(ay)
	local cy = math.cos(ay)
	local sz = math.sin(az)
	local cz = math.cos(az)
	self.r00 = cy*cz
	self.r01 = cz*sx*sy-cx*sz
	self.r02 = cx*cz*sy+sx*sz
	self.r10 = cy*sz
	self.r11 = cx*cz + sx*sy*sz
	self.r12 = -cz*sx + cx*sy*sz
	self.r20 = -sy
	self.r21 = cy*sx
	self.r22 = cx*cy
	self.r33 = 1
	return self
end

function mat4.rotation(angle_in_rad,axis)
	local c = math.cos(angle_in_rad)
	local s = math.sin(angle_in_rad)
	local n = axis:normalize()
	local x,y,z = n.x,n.y,n.z
	local out = ffi.new("mat4_t")
	out.r00 = c + x*x*(1-c)
	out.r01 = x*y*(1-c) - z*s
	out.r02 = x*z*(1-c) + y*s
	out.r10 = y*x*(1-c) + z*s
	out.r11 = c + y*y*(1-c)
	out.r12 = y*z*(1-c) - x*s
	out.r20 = z*x*(1-c) - y*s
	out.r21 = z*y*(1-c) + x*s
	out.r22 = c + z*z*(1-c)
	out.r33 = 1
	return out
end

function mat4.has_nan_or_inf(self)
	for i = 0, 15 do
		local v = self.m[i]
		if v ~= v or v == math.huge or v == -math.huge then
			return true
		end
	end
	return false
end

--@usage: local norm_mtx = mdl_mtx:inverse()
function mat4.inverse(self)
	local xx = self.r00
	local xy = self.r01
	local xz = self.r02
	local xw = self.r03
	local yx = self.r10
	local yy = self.r11
	local yz = self.r12
	local yw = self.r13
	local zx = self.r20
	local zy = self.r21
	local zz = self.r22
	local zw = self.r23
	local wx = self.r30
	local wy = self.r31
	local wz = self.r32
	local ww = self.r33
	local det = 0
	det = det + xx * (yy*(zz*ww - zw*wz) - yz*(zy*ww - zw*wy) + yw*(zy*wz - zz*wy) )
	det = det - xy * (yx*(zz*ww - zw*wz) - yz*(zx*ww - zw*wx) + yw*(zx*wz - zz*wx) )
	det = det + xz * (yx*(zy*ww - zw*wy) - yy*(zx*ww - zw*wx) + yw*(zx*wy - zy*wx) )
	det = det - xw * (yx*(zy*wz - zz*wy) - yy*(zx*wz - zz*wx) + yz*(zx*wy - zy*wx) )
	local invDet = 1/det
	local out = ffi.new("mat4_t")
	out.r00 = (yy*(zz*ww - wz*zw) - yz*(zy*ww - wy*zw) + yw*(zy*wz - wy*zz) ) * invDet
	out.r01 = -(xy*(zz*ww - wz*zw) - xz*(zy*ww - wy*zw) + xw*(zy*wz - wy*zz) ) * invDet
	out.r02 = (xy*(yz*ww - wz*yw) - xz*(yy*ww - wy*yw) + xw*(yy*wz - wy*yz) ) * invDet
	out.r03 = -(xy*(yz*zw - zz*yw) - xz*(yy*zw - zy*yw) + xw*(yy*zz - zy*yz) ) * invDet
	out.r10 = -(yx*(zz*ww - wz*zw) - yz*(zx*ww - wx*zw) + yw*(zx*wz - wx*zz) ) * invDet
	out.r11 = (xx*(zz*ww - wz*zw) - xz*(zx*ww - wx*zw) + xw*(zx*wz - wx*zz) ) * invDet
	out.r12 = -(xx*(yz*ww - wz*yw) - xz*(yx*ww - wx*yw) + xw*(yx*wz - wx*yz) ) * invDet
	out.r13 = (xx*(yz*zw - zz*yw) - xz*(yx*zw - zx*yw) + xw*(yx*zz - zx*yz) ) * invDet
	out.r20 = (yx*(zy*ww - wy*zw) - yy*(zx*ww - wx*zw) + yw*(zx*wy - wx*zy) ) * invDet
	out.r21 = -(xx*(zy*ww - wy*zw) - xy*(zx*ww - wx*zw) + xw*(zx*wy - wx*zy) ) * invDet
	out.r22 = (xx*(yy*ww - wy*yw) - xy*(yx*ww - wx*yw) + xw*(yx*wy - wx*yy) ) * invDet
	out.r23 = -(xx*(yy*zw - zy*yw) - xy*(yx*zw - zx*yw) + xw*(yx*zy - zx*yy) ) * invDet
	out.r30 = -(yx*(zy*wz - wy*zz) - yy*(zx*wz - wx*zz) + yz*(zx*wy - wx*zy) ) * invDet
	out.r31 = (xx*(zy*wz - wy*zz) - xy*(zx*wz - wx*zz) + xz*(zx*wy - wx*zy) ) * invDet
	out.r32 = -(xx*(yy*wz - wy*yz) - xy*(yx*wz - wx*yz) + xz*(yx*wy - wx*yy) ) * invDet
	out.r33 = (xx*(yy*zz - zy*yz) - xy*(yx*zz - zx*yz) + xz*(yx*zy - zx*yy) ) * invDet
	return out
end

function mat4.fromTRS(self, translation, rotation, scale)
	local t = ffi.new("mat4_t"):identity():translate(translation.x, translation.y, translation.z)
	local r = rotation:toMat4()
	local s = ffi.new("mat4_t"):identity():scale(scale.x, scale.y, scale.z)

	local trs = t * r * s

	-- Copy result into self (in-place)
	for i = 0, 15 do
		self.m[i] = trs.m[i]
	end

	return self
end

function mat4.decomposeTRS(self)
    local translation = ffi.new("vec3_t", self.r30, self.r31, self.r32)

    -- Extract basis vectors
    local x_axis = ffi.new("vec3_t", self.r00, self.r01, self.r02)
    local y_axis = ffi.new("vec3_t", self.r10, self.r11, self.r12)
    local z_axis = ffi.new("vec3_t", self.r20, self.r21, self.r22)

    local scale_x = x_axis:length()
    local scale_y = y_axis:length()
    local scale_z = z_axis:length()

    local scale = ffi.new("vec3_t", scale_x, scale_y, scale_z)

    -- Normalize rotation matrix
    x_axis = x_axis:scale(1 / scale_x)
    y_axis = y_axis:scale(1 / scale_y)
    z_axis = z_axis:scale(1 / scale_z)

    local rot_matrix = ffi.new("mat4_t")(
        x_axis.x, x_axis.y, x_axis.z, 0,
        y_axis.x, y_axis.y, y_axis.z, 0,
        z_axis.x, z_axis.y, z_axis.z, 0,
        0,        0,        0,        1
    )

    local rotation = quat.from_mat4(rot_matrix)

    return translation, rotation, scale
end

-- Decompose a column-major matrix to TRS
function mat4.decomposeTRS_CM(self)
    local translation = ffi.new("vec3_t", self.c03, self.c13, self.c23)

    local x_axis = ffi.new("vec3_t", self.c00, self.c01, self.c02)
    local y_axis = ffi.new("vec3_t", self.c10, self.c11, self.c12)
    local z_axis = ffi.new("vec3_t", self.c20, self.c21, self.c22)

    local scale_x = x_axis:length()
    local scale_y = y_axis:length()
    local scale_z = z_axis:length()

    local scale = ffi.new("vec3_t", scale_x, scale_y, scale_z)

    -- Normalize axes
    x_axis = x_axis:scale(1 / scale_x)
    y_axis = y_axis:scale(1 / scale_y)
    z_axis = z_axis:scale(1 / scale_z)

    local rot_matrix = ffi.new("mat4_t")(
        x_axis.x, y_axis.x, z_axis.x, 0,
        x_axis.y, y_axis.y, z_axis.y, 0,
        x_axis.z, y_axis.z, z_axis.z, 0,
        0,        0,        0,        1
    )

    local rotation = quat.from_mat4(rot_matrix)

    return translation, rotation, scale
end


function mat4.clone(self)
	local out = ffi.new("mat4_t")
	return out(self.r00,self.r01,self.r02,self.r03,self.r10,self.r11,self.r12,self.r13,self.r20,self.r21,self.r22,self.r23,self.r30,self.r31,self.r32,self.r33)
end

function mat4.unpack(self)
	return self.r00,self.r01,self.r02,self.r03,self.r10,self.r11,self.r12,self.r13,self.r20,self.r21,self.r22,self.r23,self.r30,self.r31,self.r32,self.r33
end

function mat4.print(self)
    print(self:to_string())
end

function mat4.to_string(self)
    return string.format(
        "[[ %.3f %.3f %.3f %.3f ]\n [ %.3f %.3f %.3f %.3f ]\n [ %.3f %.3f %.3f %.3f ]\n [ %.3f %.3f %.3f %.3f ]]",
        self.r00, self.r01, self.r02, self.r03,
        self.r10, self.r11, self.r12, self.r13,
        self.r20, self.r21, self.r22, self.r23,
        self.r30, self.r31, self.r32, self.r33
    )
end

-- assign metatables to ctypes
ffi.metatype("vec2_t",vec2_mt)
ffi.metatype("vec3_t",vec3_mt)
ffi.metatype("vec4_t",vec4_mt)
ffi.metatype("quat_t",quat_mt)
ffi.metatype("mat4_t",mat4_mt)

_G.vec2 = vec2
_G.vec3 = vec3
_G.vec4 = vec4
_G.quat = quat
_G.mat4 = mat4
