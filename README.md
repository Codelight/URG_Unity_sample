# URG Unity sample

This is a sample of using Hokuyo Electric's range finder in Unity.
It uses a sample URG communication program for C#, which is available at URG Nework.
https://sourceforge.net/p/urgnetwork/wiki/cs_sample_en/

It acquires environmental data 10 seconds after startup, and detects objects based on the difference.

It has parameters for adjustment and can align the sensor with the projected image.

TOML is used to save the configuration file.
Available from: http://paiden.github.io/Nett/