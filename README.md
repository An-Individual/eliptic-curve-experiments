# Elliptic Curve Experiments

I wanted to better understand elliptic curve encryption and learn better by doing so this is a collection of code I wrote during that process. The code related to curve math is mostly adapted from code written by others and I've included comments linking to those sources in line. Here I'm going to summarize what I've learned because that process is also helpful. But before I do that...

**Do not try to use any of the code here in real world scenarios.** I am not an expert. I know just enough to know that bad implementations of this stuff can create big security problems, but not enough not to make those mistakes. I also have not been optimizing this code. It's functional, but that's about it.

## What is an Elliptic Curve?

You can find the rote explanation in a lot of places. [Wikipedia](https://en.wikipedia.org/wiki/Elliptic_curve) is always a good start. [This](https://github.com/nakov/Practical-Cryptography-for-Developers-Book/blob/master/asymmetric-key-ciphers/elliptic-curve-cryptography-ecc.md) is also a good detailed explanation of most of the things I'm going to be talking about, and I'm going to steal a lot of numbers from it because they're good numbers. So credit the author there for most of what I'm going to say.

In extremely brief terms, they are graphs defined by the equation `y^2 = x^3 + a*x + b` with the values of `a` and `b` depending on the graph. They look something like the below, which graphs `y^2 = x^3 + 7`.

![A line on a grid descending down from the top right towards the middle, bulging around the center of the graph, then descending to the bottom right.](https://github.com/An-Individual/images/blob/main/EllipticCurve.JPG?raw=true "y^2 = x^3 + 7")

Interesting as these are, the thing we're really interested in are something called fixed field elliptic curves. These restrict the values of X and Y to integers and apply a modulo to the equation so that any time a line on the graph would move outside the field, it just wraps around the other side (this is sometimes called clock math). If we replace the above equestion with `(x^3 + 7 - y^2) % 17 = 0` we get a graph that looks like this.

![A series of dots with values between 0 and 16 on both axies. If you were to draw lines between them they'd look a little like a picture of a mountain range from across a lake, with the reflection of the mountains happening through the line where y is equal to 8.5.](https://github.com/An-Individual/images/blob/main/FixedFieldEllipticCurve.png?raw=true "(x^3 + 7 - y^2) % 17 = 0")

These points have a number of useful mathmatical properties that can be exploited.

## Point Multiplication

[Point multiplication](https://en.wikipedia.org/wiki/Elliptic_curve_point_multiplication) is a mechanism used to take one starting point on a graph and enumerate additional points based on a single integer. There are different ways to implement it. The one I've implemented is called **Double and Add** and it uses two operations.

**Important:** From this point forward you're going to see me using some familiar looking math symbols. They do not at all mean the things you are used to them meaning. I'm going to breeze past the actual math details so please refer to [this Wikipedia article](https://en.wikipedia.org/wiki/Elliptic_curve_point_multiplication) for those details if you're interested. You can also find the C# implementation in [WeierstrasCurve.cs](/ECC/WeierstrasCurve.cs). 

### Point Addition

Point addition uses the slope between two points to find a third point. Here are some examples from the point playground experiment.

```
Command: (2, 7) + (5, 8)
(12, 1)
Command: (8, 3) + (10, 2)
(12, 16)
Command: (15, 13) + (3, 0)
(1, 5)
```

### Point Doubling

Point doubling is what happens when you add a point to itself. In this case, the slope used to calculate the next point is a line tanget to the curve of the graph. Here are some more examples from the playground experiment.

```
Command: (2, 7) + (2, 7)
(12, 16)
Command: (8, 3) + (8, 3)
(5, 8)
Command: (15, 13) + (15, 13)
(2, 10)
```

### Add and Double

Contrary to what you might think, Point Multiplication doesn't use the multiplication number for multiplication purposes. Instead, the **Add and Double** method numerates all the 1s and 0s the number is made out of and uses them to map a series of doubling and adding actions that produces a specific point from an origin point.

The choice of starting point can make a big difference. Consider the following example which shows values for multiplying the point `(15, 13)` by numbers from 0 to 24.

```
Command: (15,13) * [0:24]
0: (0, 0)
1: (15, 13)
2: (2, 10)
3: (8, 3)
4: (12, 1)
5: (6, 6)
6: (5, 8)
7: (10, 15)
8: (1, 12)
9: (3, 0)
10: (1, 5)
11: (10, 2)
12: (5, 9)
13: (6, 11)
14: (12, 16)
15: (8, 14)
16: (2, 7)
17: (15, 4)
18: (0, 0)
19: (15, 13)
20: (2, 10)
21: (8, 3)
22: (12, 1)
23: (6, 6)
24: (5, 8)
```

This enumerates all the points of the curve. But you'll notice that there are only 18 distinct points. The one's from the pictured graph plus `(0,0)` which is a special point called *infinity*. After that, the pattern repeats. Now consider what happens when we use `(5,9)` as our base point.

```
Command: (5,9) * [0:7]
0: (0, 0)
1: (5, 9)
2: (5, 8)
3: (0, 0)
4: (5, 9)
5: (5, 8)
6: (0, 0)
7: (5, 9)
```

Notice that only 3 points are produced before the pattern starts repeating. This makes the choice of starting point very important.

The curves used in Eliptic Curve cryptography include not just values for `a`, `b`, and `p`, but also a starting point called `G`. The number of unique points that can produced my multiplying `G` by increasingly large numbers is called the **Order** of the curve.

## Using Elliptic Curves to Hide Information

When using elliptic curves for cryptography, one does not simply invent numbers to create a curve. There are a number of pre-defined curves with names like `brainpoolP160r1`, `nistP256`, and `secp256k1`. They will generally be defined by the following things.

* `A`: The `a` from the `y^2 = x^3 + a*x + b` equation.

* `B`: The `a` from the `y^2 = x^3 + a*x + b` equation.

* `Prime`: The `p` from the `(x^3 + a*x + b - y^2) % p = 0` equation, which should always be a prime because of the symetries it creates and the mathmatical tricks it lets you exploit.

* `Generator Point`: Also just called `G`. This is the base point from which all other points will be generated.

* `Order`: The total number of unique points that can be generated from the generator point.

* `Coefficient`: None of the curves I looked at, or math I interacted with, used this so I'll just refer to [this page](https://github.com/nakov/Practical-Cryptography-for-Developers-Book/blob/master/asymmetric-key-ciphers/elliptic-curve-cryptography-ecc.md) again for an explanation. In the cases I worked with this was always `1`.

In this system, the private key is just a number between 1 and one less than the order of the curve. The public key is just the point calculated by performing point multiplication on the generator point with the private key. It is safe for me to publish the public key because the only way to figure out what private key was used to produce it is to brute force the problem, multiplying the generator point by successively increasing numbers until you get a point that matches. Secure curves have such large orders that doing this is infeasible on anything approaching a reasonable time scale.

So how does this allow us to conceal information? Imagine we're using the small curve from the previous examples with a generator point of  `(15,13)` and I sent you the public key `(6,6)`. You could easily figure out how I got to that number by reading some of the above examples, but for now imagine that you couldn't.

You pick an arbitryar number in the private key range, say `3`, and multiply the generator point with it to get the point `(8,3)`. You also multiply my public key by the same number to get the point `(8,14)`.

```
Command: (15, 13) * 3
(8, 3)
Command: (6,6) * 3
(8, 14)
```

Now you use the data from the point `(8, 14)` to encrypt some data, but keep that point secret. You send me the encrpted data and the point `(8,3)`. I multiply that point with my private key, which is `5` and I get...

```
Command: (8,3) * 5
(8, 14)
```

The same point you got! And now I can decrypt the data.

There is a symetry here expressed as `(G * a) * b = (G * b) * a`. In this secenario, I revealed `(G * a)` to the public and you revealed `(G * b)` to the public, but at no point could anyone figure out what `a` or `b` were. But each knowing one of those values, we could both calculate the same curve point.

It is important to note that the arbitrary number chosen here (in this case `3`) can create security risks if the same number is chosen repeatedly. See the page on [Elliptic Curve Digital Signature Algorithm page](https://en.wikipedia.org/wiki/Elliptic_Curve_Digital_Signature_Algorithm) for more info.

## Key Compression

All public keys are just points on a graph, and if you look at the example graph you'll notice two things. All points share an X value with exactly one other point (if their Y value isn't 0) and of these point pairs one Y value is always even and the other is always odd.

This means that instead of writing out a public key's X and Y values in full, we can write out their X value and another small value indicating if the Y value was even or odd. From that information, we can re-calculate the Y value from the X value by calculating both possible Y values and selecting the appropriate one.

## Experiments

The solution in the repositor was written using Visual Studio 2022 Community Edition targeting .NET 7. The easiest way to interact with it is to download Visual Studio configured for .NET 7 development, open the solution, and run it. It includes a command line interface with the following features.

### Point Playground

```
Initializing Weierstras Curve...
Points are on the cuve if (x^3 + a*x + b - y2) % p == 0
Please specify a value for 'a':
0
Please specify a value for 'b':
7
Please specify a value for 'p'. Ideally this should be a prime number:
17
Initialization Complete
Formatting:
<point_name>: A string of alpha numeric characters. The first must be alphabetic.
    <number>: An integer or a hex string that starts with '0x'.
     <point>: A new point in the format (<number>,<number>) or a <point_name>.
Commands:
                             exit: Exit the program.
                             help: Repeat these instructions.
                            curve: Prints the curve information.
                             list: Lists all stored points.
                     <point name>: Prints the point stored in the specified name.
           <point name> = <point>: Stores the specified point in the given name.
                <point> + <point>: Peforms point addition and prints the result.
 <point name> = <point> + <point>: Peforms point addition storing the result.
               <point> * <number>: Peforms point multiplication and prints the result.
<point name> = <point> * <number>: Peforms point multiplication storing the result.
    <point> * [<number>:<number>]: Prints the results for multiplying the point by
                                   each integer in the given range.
Command: G = (15,13)
G = (15, 13)
Command: Q = G * 6
Q = (5, 8)
Command:
```

I used this to generate data for the examples above. It's a fairly simple tool that lets you define a curve and perform addition/doubling and multiplication operations on the points.

### WIF Parser

```
Enter a Bitcoin private key in Wallet Import Format (WIF):
KxBNk5Jj4aK7avaCBkQUX4LBQC9UccbGyvgdFNMnyXLVeQUaATNB
Public key is compressed
Private Key:
    1CA2D68540AB7ED11D24F2765D1ABDE829DCBEA4686454669D87E5C8018298F2

Public Key as Point:
    X: BED3436DE2DB1D423EB0BFEF97E501A5303B170227480BE9AE642A17E405DF43
    Y: A7C2BC7F53E1C5886B0B466AEFD6EE70BE81DCEE0B022206ED0731C6A4A86C1C

Public Key as Byte Array:
    02BED3436DE2DB1D423EB0BFEF97E501A5303B170227480BE9AE642A17E405DF43

Wallet Address:
    14rk6k2TRupdUTGfhgZz7TxzFL5qB3vtFS
```

I'm very strongly anti-blockchains. But the prevelence of Bitcoin wallet generators makes them a useful source of data for demonstrating the process of calculating public keys from private keys. This will takes a Wallet Import Formatted (WIF) private key and uses it to calculate the assocated public key and address.

In case it needs to be said, it would be very foolish to use that key for anything.

### Make Key

```
Private Key:
    24F7544BC48C09D6ABDE21B41579E3BFC317B33C401BC4F282334ABCF56AD243

Public Key as Point:
    X: D7C0FD1B9AFFFE466C44D96ED598CCA9D643B0F28EE441D5E4BE966D445E9340
    Y: 15D471CDFEDAFA346A8A5A486BEBB1058622FA56B15DC5D693DF8B01FC1C1B6A

Uncompressed Public Key:
    04D7C0FD1B9AFFFE466C44D96ED598CCA9D643B0F28EE441D5E4BE966D445E934015D471CDFEDAFA346A8A5A486BEBB1058622FA56B15DC5D693DF8B01FC1C1B6A

Compressed Public Key:
    02D7C0FD1B9AFFFE466C44D96ED598CCA9D643B0F28EE441D5E4BE966D445E9340
```

Used to generate keys that can be used in the other experiments. I would not trust this as a source for generating truely secure keys.

### Signer

```
Enter a private key as a hex string:
24F7544BC48C09D6ABDE21B41579E3BFC317B33C401BC4F282334ABCF56AD243
Enter the path to the file to generate a signature for:
C:\logfile.log
Reading file...
Hashing file with SHA256...
Generating signature...
Signature:
    1234E3D340D0B4DA358FD7AC5D7C5D4261DE70A15186059EC8C25D46E2627F4E56E61E3DDD0F63D5CD63F20DAF44210813469CCA2FE0555E64F97730270AB167
```

Uses a provided private key to generate a signature for a given file.

### Validator

```
Enter the public key as a hex string:
02D7C0FD1B9AFFFE466C44D96ED598CCA9D643B0F28EE441D5E4BE966D445E9340
Enter the signature as a hex string:
1234E3D340D0B4DA358FD7AC5D7C5D4261DE70A15186059EC8C25D46E2627F4E56E61E3DDD0F63D5CD63F20DAF44210813469CCA2FE0555E64F97730270AB167
Enter the path to the file that was signed:
C:/logfile.log
Reading file...
Hashing file with SHA256...
Validating signature...
Signature is Valid
```

Validates that the given signature was created by the given public key and is a signature of the given file.