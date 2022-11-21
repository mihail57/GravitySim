
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace GravitySim
{
    public class Body
    {
        [JsonConstructor]
        public Body(int id, double[] coords, string name, Vector speed, double mass, int clr) //Версия для десериализации
        {
            this.name = name;
            this.coords = coords;
            this.mass = mass;
            this.speed = speed;
            this.acceleration = new Vector(0, 0);
            this.color = Color.FromArgb(clr);
            forces = new List<Vector>();
            this.id = id;
        }

        public Body(string s, double x, double y, double mass, Vector startSpeed, Color color, int id)
        {
            name = s;
            this.coords[0] = x;
            this.coords[1] = y;
            visualCoords[0] = x;
            visualCoords[1] = y;
            this.mass = mass;
            this.speed = startSpeed;
            this.acceleration = new Vector(0,0);
            this.color = color;
            forces = new List<Vector>();
            this.id = id;
        }

        public Body(Body old)
        {
            name = old.name;
            coords[0] = old.coords[0];
            visualCoords[0] = old.visualCoords[0];
            coords[1] = old.coords[1];
            visualCoords[1] = old.visualCoords[1];
            color = old.color;
            speed = old.speed;
            id = old.id;
            mass = old.mass;
            tracing = new List<PointF>(old.tracing);
            forces = new List<Vector>(old.forces);
            acceleration = new Vector(old.acceleration.modulus, old.acceleration.angle);
        }

        public int id;

        public double[] coords = new double[2];

        [JsonIgnore]
        public double[] visualCoords = new double[2];
        [JsonIgnore]
        public List<PointF> tracing = new List<PointF>();
        [JsonIgnore]
        public PointF rotatePoint = new PointF();

        public string name;
        [JsonIgnore]
        public Vector acceleration;
        public Vector speed;
        public double mass;
        [JsonIgnore]
        public Color color;
        public int clr = 0; //Структура Color не может быть сериализована
        [JsonIgnore]
        public List<Vector> forces;

        public const double au = 149597870700;
        public const double scale = au * 3.52 / 200;
        public const double gravityConst = 6.6743;

        public const double eps = 0.1;

        public static double[] CalculateVisualCoords(Body zeroPoint, double[] oldCoords, double scale)
        {
            double[] result = new double[2];
            result[0] = zeroPoint.visualCoords[0] + oldCoords[0] * scale;
            result[1] = zeroPoint.visualCoords[1] + oldCoords[1] * scale;
            return result;
        }

        public static void CalculatePos(Vector acceleration, int timePerTick, ref double[] coords, ref Vector speed)
        {
            double res = acceleration.modulus * timePerTick * timePerTick / 2;
            double x = (res * Math.Sin(acceleration.angle) + speed.modulus * Math.Sin(speed.angle) * timePerTick) / scale; //X axis
            double y = (res * Math.Cos(acceleration.angle) + speed.modulus * Math.Cos(speed.angle) * timePerTick) / scale; //Y axis
            coords[0] += x; 
            coords[1] += y; 
            Vector speedFromAcc = new Vector(acceleration.modulus * timePerTick, acceleration.angle);
            speed = Vector.Sum(speed, speedFromAcc);
        }

        public static List<PointF> AddPoint(PointF coords, List<PointF> list)
        {
            for (int i = 0; i < 3; i++)
            {
                if (list.Count > 2999) list.RemoveAt(0);
                list.Add(coords);
            }
            return list;
        }

        public static double[] RecalculateZeroPos(Body zero, double scale, float xMove, float yMove, int xMouse, int yMouse, bool scroll, bool locked, double[] lockPoint = null)
        {
            zero.visualCoords[0] += xMove;
            zero.visualCoords[1] += yMove;

            if (scroll)
            {
                if (!locked)
                {
                    zero.visualCoords[0] *= (float)scale;
                    zero.visualCoords[0] -= xMouse * ((float)scale - 1);
                    zero.visualCoords[1] *= (float)scale;
                    zero.visualCoords[1] -= yMouse * ((float)scale - 1);
                }
                else
                {
                    zero.visualCoords[0] *= (float)scale;
                    zero.visualCoords[0] -= lockPoint[0] * ((float)scale - 1);
                    zero.visualCoords[1] *= (float)scale;
                    zero.visualCoords[1] -= lockPoint[1] * ((float)scale - 1);
                }
            }

            return new double[2] { zero.visualCoords[0], zero.visualCoords[1] };
        }

        public static void Draw(Body body, Body zeroPoint, double scale, Graphics g, ref PointF point, bool drawVector = false, bool drawAcc = false)
        {
            body.visualCoords = CalculateVisualCoords(zeroPoint, body.coords, scale);

            g.DrawRectangle(new Pen(body.color, 3), (float)body.visualCoords[0], (float)body.visualCoords[1], 2, 2);

            PointF[] tr = new PointF[body.tracing.Count + 1];
            for (int i = 0; i < body.tracing.Count; i++)
            {
                double[] trCoords = new double[2] { body.tracing[i].X, body.tracing[i].Y };
                trCoords = CalculateVisualCoords(zeroPoint, trCoords, scale);
                tr[i] = new PointF((float)trCoords[0], (float)trCoords[1]);
            }
            tr[tr.Length - 1] = new PointF((float)body.visualCoords[0], (float)body.visualCoords[1]);

            g.DrawBeziers(new Pen(body.color, 1), tr);

            if (drawAcc && body.id != 0) Vector.DrawVector(body.acceleration, body.visualCoords, g, Color.Gray, ref point);
            if (drawVector && body.id != 0) Vector.DrawVector(body.speed, body.visualCoords, g, Color.Black, ref point);
        }

        public static Vector Gravity(Body body1, Body body2)
        {
            double prX = (body2.coords[0] - body1.coords[0]) * scale;
            double prY = (body2.coords[1] - body1.coords[1]) * scale;
            double distance = Math.Sqrt(prX * prX + prY * prY);
            double modulus = gravityConst * body1.mass * body2.mass / (distance * distance + eps * eps);

            double sumAngle = Math.Asin(prX / distance);
            if (prY < 0 && prX < 0) sumAngle = -Math.Acos(prY / distance) + 2 * Math.PI;
            else if (prY < 0 && prX > 0) sumAngle = Math.Acos(prY / distance);
            else if (prX < 0 && prY > 0) sumAngle = Math.Asin(prX / distance);

            return new Vector(modulus, sumAngle);
        }

        public static int LocateClick(float x, float y, List<Body> bodies)
        {
            int result = -1;
            for (int i = 1; i < bodies.Count; i++) if (x >= bodies[i].visualCoords[0] - 2 && x < bodies[i].visualCoords[0] + 5 && y >= bodies[i].visualCoords[1] - 2 && y < bodies[i].visualCoords[1] + 5) result = i;
            return result;
        }
    }

    public struct Vector
    {
        public Vector(double a, double b)
        {
            this.modulus = a;
            this.angle = b;
        }
        public double modulus;
        public double angle;

        public static Vector Sum(Vector v1, Vector v2)
        {
            double projectionX = v1.modulus * Math.Sin(v1.angle) + v2.modulus * Math.Sin(v2.angle);
            double projectionY = v1.modulus * Math.Cos(v1.angle) + v2.modulus * Math.Cos(v2.angle);
            double sumModulus = Math.Sqrt(projectionX * projectionX + projectionY * projectionY);
            if (projectionX == 0 && projectionY == 0) return new Vector(0, 0);
            double sumAngle = Math.Asin(projectionX / sumModulus);
            if (projectionY < 0 && projectionX < 0) sumAngle = -Math.Acos(projectionY / sumModulus) + 2 * Math.PI; 
            else if (projectionY < 0 && projectionX > 0) sumAngle = Math.Acos(projectionY / sumModulus);
            else if (projectionX < 0 && projectionY > 0) sumAngle = Math.Asin(projectionX / sumModulus);
            return new Vector(sumModulus, sumAngle);
        }

        public static Vector Sum(List<Vector> vectors)
        {
            double sumModulus = vectors[0].modulus;
            double sumAngle = vectors[0].angle;
            for (int i = 1; i < vectors.Count; i++)
            {
                double projectionX = sumModulus * Math.Sin(sumAngle) + vectors[i].modulus * Math.Sin(vectors[i].angle);
                double projectionY = sumModulus * Math.Cos(sumAngle) + vectors[i].modulus * Math.Cos(vectors[i].angle);
                sumModulus = Math.Sqrt(projectionX * projectionX + projectionY * projectionY);
                if (projectionX == 0 && projectionY == 0) { sumModulus = 0; sumAngle = 0; }
                sumAngle = Math.Asin(projectionX / sumModulus);
                if (projectionY < 0 && projectionX < 0) sumAngle = -Math.Acos(projectionY / sumModulus) + 2 * Math.PI;
                else if (projectionY < 0 && projectionX > 0) sumAngle = Math.Acos(projectionY / sumModulus);
                else if (projectionX < 0 && projectionY > 0) sumAngle = Math.Asin(projectionX / sumModulus);
            }
            return new Vector(sumModulus, sumAngle);
        }

        public static Vector NegativeVector(Vector v)
        {
            return new Vector(v.modulus, Math.PI + v.angle);
        }

        public static void DrawVector(Vector vector, double[] position, Graphics g, Color col, ref PointF point)
        {
            point = new PointF((float)(20 * Math.Sin(vector.angle) + position[0] + 1), (float)(20 * Math.Cos(vector.angle) + position[1] + 1));
            g.DrawLine(new Pen(col, 1), new PointF((float)position[0] + 1, (float)position[1] + 1), point);
        }

        public static Vector ChangeAngle(Vector vector, PointF newPos, double[] bodyPoint)
        {

            double b = Math.Abs(newPos.X - bodyPoint[0]);
            double a = Math.Sqrt(Math.Pow(newPos.X - bodyPoint[0], 2) + Math.Pow(newPos.Y - bodyPoint[1], 2));
            double r = b / a;

            double newAngle = -Math.Acos(r) + Math.PI/2;
            if (newPos.Y - bodyPoint[1] <= 0 && newPos.X - bodyPoint[0] <= 0) newAngle = -Math.Acos(r) - Math.PI / 2;
            else if (newPos.Y - bodyPoint[1] <= 0 && newPos.X - bodyPoint[0] >= 0) newAngle = Math.Acos(r) + Math.PI / 2;
            else if (newPos.X - bodyPoint[0] <= 0 && newPos.Y - bodyPoint[1] >= 0) newAngle = Math.Acos(r) - Math.PI / 2;


            if (a == 0) return vector;

            return new Vector(vector.modulus, newAngle);
        }
    }

    public class SimulationProperties
    {
        [JsonConstructor]
        public SimulationProperties(int calcPerTick, int timePerCalc, int time)
        {
            this.calcPerTick = calcPerTick;
            this.timePerCalc = timePerCalc;
            this.time = time;
        }

        public int calcPerTick;
        public int timePerCalc;
        public int time;
    }
}
