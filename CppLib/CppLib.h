#pragma once

#define PI 3.14159265358979323846

#include <cmath>

using namespace System;

namespace CppLib {
	public ref class Vector {
	public:

		Vector(double a, double b);
		~Vector();

		double modulus;
		double angle;

		static Vector^ Sum(Vector^ v1, Vector^ v2);
		static Vector^ NegativeVector(Vector^ v) { return gcnew Vector(v->modulus, v->angle + PI); }
		static Vector^ ChangeAngle(Vector^ vector, float newX, float newY, array<double>^ coords);
		static Vector^ Gravity(array<double>^ b1, double mass1, array<double>^ b2, double mass2, double scale, double gravconst, double eps);
		static array<double>^ CalculatePos(Vector^ acc, int timePerTick, array<double>^ coord, Vector^ speed, double scale);
		static Vector^ CalculateSpeed(Vector^ acc, Vector^ speed, int timePerTick);

	private:
	};
}
