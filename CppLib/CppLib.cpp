#include "pch.h"

#include "CppLib.h"

using namespace CppLib;
using namespace System;

Vector::Vector(double a, double b) {
	modulus = a;
	angle = b;
}

Vector::~Vector() {
}

Vector^ Vector::Sum(Vector^ v1, Vector^ v2) {
	double prX = v1->modulus * sin(v1->angle) + v2->modulus * sin(v2->angle);
	double prY = v1->modulus * cos(v1->angle) + v2->modulus * cos(v2->angle);
	double sumMod = sqrt(prX * prX + prY * prY);

	if (sumMod == 0) return gcnew Vector(0, 0);

	double sumAngle = asin(prX / sumMod);
	if (prX < 0 && prY < 0) sumAngle = -acos(prY / sumMod) + 2 * PI;
	else if (prX > 0 && prY < 0) sumAngle = acos(prY / sumMod);
	else if (prX < 0 && prY > 0) sumAngle = asin(prX / sumMod);

	return gcnew Vector(sumMod, sumAngle);
}

Vector^ Vector::ChangeAngle(Vector^ vector, float newX, float newY, array<double>^ coords) {
	double b = abs(newX - coords[0]);
	double a = sqrt(pow(newX - coords[0], 2) + pow(newY - coords[1], 2));

	if (a == 0) return vector;

	double newAngle = -acos(b / a) + PI / 2;
	if (newY - coords[1] <= 0 && newX - coords[0] <= 0) newAngle = -acos(b / a) - PI / 2;
	else if (newY - coords[1] <= 0 && newX - coords[0] >= 0) newAngle = acos(b / a) + PI / 2;
	if (newY - coords[1] >= 0 && newX - coords[0] <= 0) newAngle = acos(b / a) - PI / 2;

	return gcnew Vector(vector->modulus, newAngle);
}

Vector^ Vector::Gravity(array<double>^ b1, double mass1, array<double>^ b2, double mass2, double scale, double gravconst, double eps) {
	double prX = (b2[0] - b1[0]) * scale;
	double prY = (b2[1] - b1[1]) * scale;
	double distance = sqrt(prX * prX + prY * prY);
	double mod = gravconst * mass1 * mass2 / (distance * distance + eps * eps);

	double sumAngle = asin(prX / distance);
	if (prX < 0 && prY < 0) sumAngle = -acos(prY / distance) + 2 * PI;
	else if (prX > 0 && prY < 0) sumAngle = acos(prY / distance);
	else if (prX < 0 && prY > 0) sumAngle = asin(prX / distance);

	return gcnew Vector(mod, sumAngle);
}

array<double>^ Vector::CalculatePos(Vector^ acc, int timePerTick, array<double>^ coord, Vector^ speed, double scale) {
	const int size = 2;
	array<double>^ result = gcnew array<double>(size);
	double res = acc->modulus * timePerTick * timePerTick / 2;
	double x = (res * sin(acc->angle) + speed->modulus * sin(speed->angle) * timePerTick) / scale;
	double y = (res * cos(acc->angle) + speed->modulus * cos(speed->angle) * timePerTick) / scale;
	result[0] = coord[0] + x;
	result[1] = coord[1] + y;
	return result;
}

Vector^ Vector::CalculateSpeed(Vector^ acc, Vector^ speed, int timePerTick) {
	Vector^ from_acc = gcnew Vector(acc->modulus * timePerTick, acc->angle);
	return Sum(speed, from_acc);
}

