import React, { useMemo, useState } from 'react';
import { FlatList, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { CategoryCard } from '@/components/home/CategoryCard';
import { SearchBar } from '@/components/ui/SearchBar';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { PRACTICE_AREA_CATEGORIES } from '@/constants/practiceAreas';
import type { HomeStackParamList } from '@/navigation/types';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<HomeStackParamList, 'Categories'>;

export function CategoriesScreen({ navigation }: Props) {
  const theme = useTheme();
  const [query, setQuery] = useState('');

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return PRACTICE_AREA_CATEGORIES;
    return PRACTICE_AREA_CATEGORIES.filter(
      (c) => c.name.toLowerCase().includes(q) || c.shortName.toLowerCase().includes(q),
    );
  }, [query]);

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']}>
      <ScreenHeader title="Categories" subtitle="Practice areas" onBack={() => navigation.goBack()} />
      <SearchBar value={query} onChangeText={setQuery} placeholder="Filter categories" />
      <FlatList
        data={filtered}
        keyExtractor={(item) => item.id}
        numColumns={3}
        columnWrapperStyle={{ gap: theme.spacing.sm }}
        contentContainerStyle={{
          gap: theme.spacing.sm,
          paddingTop: theme.spacing.md,
          paddingBottom: theme.spacing.xl,
        }}
        renderItem={({ item }) => (
          <CategoryCard
            category={item}
            style={{ flex: 1 }}
            onPress={() =>
              navigation.navigate('CategoryLawyers', {
                specializationId: item.id,
                title: item.name,
              })
            }
          />
        )}
        ListEmptyComponent={
          <Text variant="body" color="secondary" align="center">
            No categories match your filter.
          </Text>
        }
      />
    </Screen>
  );
}
