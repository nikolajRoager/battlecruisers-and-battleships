import numpy as np
import matplotlib.pyplot as plt


x_entry = 'normal_displacement'
y_entry = 'power_to_weight'

data = np.genfromtxt('ships_normal_power.csv', delimiter=',', names=True, dtype=None, encoding='utf-8')
min_year =np.min(data['year'])
max_year =np.max(data['year'])+1


step=100;

for year in range(min_year,max_year,step):
    print(year,' to ',year+step);
    data = np.genfromtxt('ships_normal_power.csv', delimiter=',', names=True, dtype=None, encoding='utf-8')

    data = data[data['year']<year+step];

    countries = np.array(data['navy'])
    types = np.array(['BB','BC'])
    unique_countries = np.unique(countries)



    #Create a dictionaries to store data for each country
    grouped_data = {country: data[countries == country] for country in unique_countries}

    for country in unique_countries:

        col = 'red'
        if (country=='France'):
            col = 'blue'
        if (country=='United States'):
            col = 'dodgerblue'
        if (country=='Argentina'):
            col = 'skyblue'
        if (country=='Australia'):
            col = 'red'
        if (country=='Australia'):
            col = 'crimson'
        if (country=='Austria-Hungary'):
            col = 'yellow'
        if (country=='Brazil'):
            col = 'forestgreen'
        if (country=='Chile'):
            col = 'lightcoral'
        if (country=='Empire of Japan'):
            col = 'pink'
        if (country=='German Empire'):
            col = 'grey'
        if (country=='Kingdom of Italy'):
            col = 'lime'
        if (country=='Nazi Germany'):
            col = 'black'
        if (country=='Netherlands'):
            col = 'orange'
        if (country=='Spain'):
            col = 'gold'
        if (country=='Russian Empire'):
            col = 'green'
        if (country=='Soviet Union'):
            col = 'crimson'

        Data = grouped_data [country];
        #split by type as well

        Types = np.array(Data['type'])

        #Create a dictionaries to store data for each country
        typed_data = {Type: Data[Types == Type] for Type in types}
        for t in types:
            name = np.array(typed_data[t]['name'])
            x = np.array(typed_data[t][x_entry])
            y = np.array(typed_data[t][y_entry])
            launch = np.array(typed_data[t]['year'])

            sorted_indices = np.argsort(launch)
            x = x[sorted_indices]
            y = y[sorted_indices]
            name = name[sorted_indices]
            launch = launch[sorted_indices]

            x=x[launch<year+step]
            y=y[launch<year+step]
            name=name[launch<year+step]
            launch=launch[launch<year+step]

            marker = 'o'
            if t=='BC':
                marker='*'


            #Stupid hack to make the names of ships in the same class appear next to each other, offset them a tiny bit, reset offset each year
            offset = 0;
            prevyear = year;
            prevy = 0;
            if len(name)>0:
                plt.plot(x, y,label=country+' '+t,color=col,marker=marker)
                for i in range (0,len(x)):
                    if launch[i]>=year:
                        if prevyear!=launch[i]:
                            prevyear=launch[i]
                            offset=0
      #                  plt.text(x[i],y[i]+offset,name[i])

                        offset-=0.01
                        prevy =y[i]

    title=str(year)+' to '+str(year+step);
    plt.title(title)
    plt.xlabel(x_entry)
    plt.ylabel(y_entry)
    plt.legend()
    plt.show()
